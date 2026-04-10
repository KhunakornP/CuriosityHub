using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// Register RabbitMQ Connection as a Singleton so it stays connected and lists the publisher
builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "rabbitmq" });
builder.Services.AddSingleton<IConnection>(sp => 
{
    var factory = sp.GetRequiredService<IConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

var app = builder.Build();

// Ensure connection is established on startup
app.Services.GetRequiredService<IConnection>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/video", async (HttpContext context, IHttpClientFactory httpClientFactory, IConnection rabbitConnection) =>
{
    var videoIdStr = context.Request.Query["videoId"].ToString();
    if (string.IsNullOrWhiteSpace(videoIdStr))
    {
        videoIdStr = context.Request.Headers["videoId"].ToString();
    }

    if (string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing videoId in query or header");
    }

    // Publish view event to RabbitMQ
    try
    {
        using var channel = await rabbitConnection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: "views", durable: true, exclusive: false, autoDelete: false, arguments: null);
        
        // Viewer ID can come from a header or "Viewer" if unauthenticated
        var viewer = context.Request.Headers["viewerId"].ToString();
        if (string.IsNullOrWhiteSpace(viewer)) viewer = "Viewer";

        var message = $"{viewer} views {videoIdStr}";
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "views", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: body);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to publish view message to RabbitMQ.");
    }

    var videoStorageUrl = "http://video-storage:8080/video";
    using var httpClient = httpClientFactory.CreateClient();
    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, videoStorageUrl);
    
    // Pass the videoId in the header for the storage microservice
    requestMessage.Headers.Add("videoId", videoIdStr);

    // Forward the Range header if requested (useful for streaming seeking)
    if (context.Request.Headers.TryGetValue("Range", out var rangeHeader))
    {
        requestMessage.Headers.TryAddWithoutValidation("Range", rangeHeader.ToString());
    }

    using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers)
    {
        if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
        context.Response.Headers.Append(header.Key, header.Value.ToArray());
    }
    foreach (var header in response.Content.Headers)
    {
        context.Response.Headers.Append(header.Key, header.Value.ToArray());
    }

    await response.Content.CopyToAsync(context.Response.Body);
    return EmptyHttpResult.Instance;
});

app.MapGet("/recent-videos", async () =>
{
    // Connect to MongoDB
    var mongoClient = new MongoClient("mongodb://mongodb:27017");
    var database = mongoClient.GetDatabase("video_db"); // Connect to your designated database
    var collection = database.GetCollection<BsonDocument>("videos");

    // Fetch the 10 most recent videos sorting by ObjectId descending
    var recentVideos = await collection.Find(new BsonDocument())
        .Sort(Builders<BsonDocument>.Sort.Descending("_id"))
        .Limit(10)
        .ToListAsync();
    
    // Select to standard dictionary list to return as JSON
    var json = recentVideos.Select(x => x.ToDictionary()).ToList();
    return Results.Ok(json);
});

app.Run();
