using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/upload", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var objectId = ObjectId.GenerateNewId().ToString();

    var videoStorageUrl = "http://video-storage:8080/upload";
    
    using var httpClient = httpClientFactory.CreateClient();
    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, videoStorageUrl);
    
    requestMessage.Headers.Add("videoId", objectId);
    
    if (context.Request.Headers.TryGetValue("file-name", out var fileName))
    {
        requestMessage.Headers.TryAddWithoutValidation("file-name", fileName.ToString());
    }
    
    // Copy the Request Body directly to the forwarding request
    requestMessage.Content = new StreamContent(context.Request.Body);
    requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType ?? "video/mp4");

    using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
    
    if (response.IsSuccessStatusCode)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "video_uploaded_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var eventMessage = new { VideoId = objectId, Status = "Uploaded" };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage));

            await channel.BasicPublishAsync(exchange: string.Empty,
                                 routingKey: "video_uploaded_queue",
                                 mandatory: false,
                                 basicProperties: new RabbitMQ.Client.BasicProperties(),
                                 body: body);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to publish to RabbitMQ");
        }
    }

    // Proxy the response back to the client
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers)
    {
        if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        context.Response.Headers.Append(header.Key, header.Value.ToArray());
    }
    foreach (var header in response.Content.Headers)
    {
        context.Response.Headers.Append(header.Key, header.Value.ToArray());
    }
    await response.Content.CopyToAsync(context.Response.Body);
});

app.Run();
