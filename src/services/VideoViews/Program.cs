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

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // message format: "{viewer} views {videoId}"
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
