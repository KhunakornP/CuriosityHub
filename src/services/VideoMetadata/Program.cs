using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("video_db").GetCollection<VideoMetadata>("metadata");
});

// Register RabbitMQ Hosted Service
builder.Services.AddHostedService<MetadataRabbitMqListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// GET /metadata endpoint
app.MapGet("/metadata", async (string videoId, IMongoCollection<VideoMetadata> collection) =>
{
    if (string.IsNullOrWhiteSpace(videoId))
        return Results.BadRequest("videoId is required");

    var metadata = await collection.Find(v => v.VideoId == videoId).FirstOrDefaultAsync();
    
    if (metadata == null) 
    {
        return Results.NotFound(new { message = "Metadata not found for the given videoId" });
    }

    return Results.Ok(metadata);
})
.WithName("GetVideoMetadata");

app.MapPut("/update", async ([FromBody] UpdateVideoMetadataRequest request, IMongoCollection<VideoMetadata> collection) =>
{
    if (string.IsNullOrWhiteSpace(request.VideoId)) return Results.BadRequest("VideoId is required");

    var update = Builders<VideoMetadata>.Update
        .Set(v => v.Title, request.Title)
        .Set(v => v.Description, request.Description);

    var result = await collection.UpdateOneAsync(v => v.VideoId == request.VideoId, update);
    if (result.MatchedCount == 0) return Results.NotFound("Video not found");

    return Results.Ok(new { message = "Metadata updated successfully" });
});

app.Run();

public class UpdateVideoMetadataRequest
{
    public string VideoId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class VideoMetadataDto
{
    public string VideoId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PublisherId { get; set; }
}

public class VideoMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string VideoId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double TotalDuration { get; set; }
    public string? Resolution { get; set; }
    public string? PublisherId { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class MetadataRabbitMqListener : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IChannel? _channel;

    public MetadataRabbitMqListener(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitHostname = _configuration.GetConnectionString("RabbitMq") ?? "rabbitmq";
        var factory = new ConnectionFactory { HostName = rabbitHostname };
        
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(queue: "video_metadata", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue: "upload_replies", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            JsonElement payload;
            try
            {
                payload = JsonSerializer.Deserialize<JsonElement>(message);
                var videoId = payload.GetProperty("VideoId").GetString() ?? throw new Exception("VideoId is required");
                var title = payload.GetProperty("Title").GetString() ?? "Unknown";
                var description = payload.TryGetProperty("Description", out var descProp) ? descProp.GetString() : null;

                var newMetadata = new VideoMetadata
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    VideoId = videoId,
                    Title = title,
                    Description = description,
                    TotalDuration = 0, // Placeholder
                    Resolution = "Unknown",
                    PublisherId = "AnonymousUser",
                    PublishedAt = DateTime.UtcNow
                };

                using var scope = _serviceProvider.CreateScope();
                var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoMetadata>>();

                // Step 3: Try inserting metadata
                await collection.InsertOneAsync(newMetadata, cancellationToken: stoppingToken);

                // Saga Reply: Success
                var reply = new { VideoId = videoId, Service = "Metadata", Status = "Success" };
                var replyBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reply));
                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "upload_replies", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: replyBody, cancellationToken: stoppingToken);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Error processing metadata for video: {ex.Message}");
                try
                {
                    // Saga Reply: Error
                    if (!string.IsNullOrEmpty(message))
                    {
                        var failPayload = JsonSerializer.Deserialize<JsonElement>(message);
                        if (failPayload.TryGetProperty("VideoId", out var vIdProp))
                        {
                            var reply = new { VideoId = vIdProp.GetString(), Service = "Metadata", Status = "Error" };
                            var replyBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reply));
                            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "upload_replies", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: replyBody, cancellationToken: stoppingToken);
                        }
                    }
                }
                catch { /* Logging error on rollback failure */ }
            }
            finally
            {
                if (_channel is not null)
                {
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
            }
        };

        await _channel.BasicConsumeAsync(queue: "video_metadata", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        
        // Wait until cancellation applies via Task completion
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken: stoppingToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken: stoppingToken);
        await base.StopAsync(stoppingToken);
    }
}
