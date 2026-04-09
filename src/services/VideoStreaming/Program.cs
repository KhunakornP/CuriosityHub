using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/video", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
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
