using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

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
    return client.GetDatabase("videoviews").GetCollection<VideoViewCount>("views");
});

// Register RabbitMQ Hosted Service
builder.Services.AddHostedService<ViewsRabbitMqListener>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// GET /views endpoint
app.MapGet("/views", async (string videoId, IMongoCollection<VideoViewCount> collection) =>
{
    if (string.IsNullOrWhiteSpace(videoId))
        return Results.BadRequest("videoId is required");

    var video = await collection.Find(v => v.VideoId == videoId).FirstOrDefaultAsync();
    
    return Results.Ok(new { videoId = videoId, totalViews = video?.TotalViews ?? 0 });
})
.WithName("GetVideoViews");

app.MapGet("/video-views", async ([FromQuery] string[] videoIds, IMongoCollection<VideoViewCount> collection) =>
{
    if (videoIds == null || !videoIds.Any())
        return Results.BadRequest("videoIds is required");

    var filter = Builders<VideoViewCount>.Filter.In(v => v.VideoId, videoIds);
    var viewsList = await collection.Find(filter).ToListAsync();
    
    var response = videoIds.Select(id => 
    {
        var viewCount = viewsList.FirstOrDefault(v => v.VideoId == id);
        return new { videoId = id, totalViews = viewCount?.TotalViews ?? 0 };
    });
    
    return Results.Ok(response);
});

app.Run();

public class VideoViewCount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string VideoId { get; set; } = null!;
    public long TotalViews { get; set; }
}

public class ViewsRabbitMqListener : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IChannel? _channel;

    public ViewsRabbitMqListener(IConfiguration configuration, IServiceProvider serviceProvider)
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

        await _channel.QueueDeclareAsync(queue: "views", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue: "upload_replies", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // message format from view client: "{viewer} views {videoId}"
                // message format from initialization: JSON with VideoId
                if (message.Contains(" views "))
                {
                    var parts = message.Split(" views ");
                    if (parts.Length == 2)
                    {
                        var videoId = parts[1].Trim();

                        using var scope = _serviceProvider.CreateScope();
                        var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoViewCount>>();

                        var update = Builders<VideoViewCount>.Update.Inc(x => x.TotalViews, 1);
                        var options = new UpdateOptions { IsUpsert = true };

                        await collection.UpdateOneAsync(x => x.VideoId == videoId, update, options, cancellationToken: stoppingToken);
                    }
                }
                else 
                {
                    // Initialization from video upload
                    try 
                    {
                        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(message);
                        if (payload.TryGetProperty("VideoId", out var vIdProp)) 
                        {
                            var videoId = vIdProp.GetString();
                            using var scope = _serviceProvider.CreateScope();
                            var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoViewCount>>();

                            var newViewCount = new VideoViewCount {
                                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                                VideoId = videoId,
                                TotalViews = 0
                            };
                            
                            // Step 4: Try inserting views tracking entry
                            await collection.InsertOneAsync(newViewCount, cancellationToken: stoppingToken);
                            
                            // Emit success
                            var reply = new { VideoId = videoId, Service = "Views", Status = "Success" };
                            var replyBody = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(reply));
                            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "upload_replies", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: replyBody, cancellationToken: stoppingToken);
                        }
                    }
                    catch {
                        // Might already exist or failed parse, ignore gracefully for initialization
                        try {
                            var p = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(message);
                            if (p.TryGetProperty("VideoId", out var vId))
                            {
                                var reply = new { VideoId = vId.GetString(), Service = "Views", Status = "Error" };
                                var replyBody = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(reply));
                                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "upload_replies", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: replyBody, cancellationToken: stoppingToken);
                            }
                        } catch { } // Totally unparseable
                    }
                }
            }
            finally
            {
                if (_channel is not null)
                {
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
            }
        };

        await _channel.BasicConsumeAsync(queue: "views", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        
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
