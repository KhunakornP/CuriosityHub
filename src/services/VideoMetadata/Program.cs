using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

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

app.Run();

public class VideoMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string VideoId { get; set; } = null!;
    public string? Description { get; set; }
    public double TotalDuration { get; set; }
    public string? Resolution { get; set; }
    public string? PublisherId { get; set; }
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
        var rabbitHostname = _configuration.GetConnectionString("RabbitMq") ?? "localhost";
        var factory = new ConnectionFactory { HostName = rabbitHostname };
        
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(queue: "video_metadata", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // Parse message to get video details (Template code)
                // Expected format/logic to parse metadata
                var videoId = "extracted_video_id"; // Replace with actual parsing logic based on your message structure
                
                var newMetadata = new VideoMetadata
                {
                    VideoId = videoId,
                    Description = "Parsed description template",
                    TotalDuration = 120.5,
                    Resolution = "1920x1080",
                    PublisherId = "Publisher_UUID"
                };

                using var scope = _serviceProvider.CreateScope();
                var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoMetadata>>();

                var update = Builders<VideoMetadata>.Update
                    .Set(x => x.Description, newMetadata.Description)
                    .Set(x => x.TotalDuration, newMetadata.TotalDuration)
                    .Set(x => x.Resolution, newMetadata.Resolution)
                    .Set(x => x.PublisherId, newMetadata.PublisherId);
                
                var options = new UpdateOptions { IsUpsert = true };

                await collection.UpdateOneAsync(x => x.VideoId == newMetadata.VideoId, update, options, cancellationToken: stoppingToken);
            }
            catch(Exception ex)
            {
                // Handle parsing or db exception
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
