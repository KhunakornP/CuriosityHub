using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// Register Saga State Tracker
builder.Services.AddSingleton<SagaTracker>();

// Register Saga Orchestrator Listener
builder.Services.AddHostedService<UploadWorkflowOrchestrator>();

// Register RabbitMQ Connection
builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "rabbitmq" });
builder.Services.AddSingleton<IConnection>(sp => 
{
    var factory = sp.GetRequiredService<IConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

var app = builder.Build();

app.Services.GetRequiredService<IConnection>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/upload", async ([FromForm] string title, [FromForm] string? description, [FromForm] string? publisherId, IFormFile video, IFormFile? thumbnail, IHttpClientFactory httpClientFactory, IConnection rabbitConnection, SagaTracker tracker) =>
{
    if (video == null || video.Length == 0) return Results.BadRequest("No video provided.");
    if (string.IsNullOrWhiteSpace(title)) return Results.BadRequest("Title is required.");

    var videoId = ObjectId.GenerateNewId().ToString();
    var videoStorageUrl = "http://video-storage:8080/upload";
    
    // Register the saga state before upload
    tracker.Sagas[videoId] = new SagaState { VideoId = videoId, Title = title, PublisherId = publisherId ?? "AnonymousUser" };

    using var client = httpClientFactory.CreateClient();

    // Step 1: Upload to Storage (Mediator start)
    using var stream = video.OpenReadStream();
    using var content = new StreamContent(stream);
    var request = new HttpRequestMessage(HttpMethod.Post, videoStorageUrl)
    {
        Content = content
    };
    request.Headers.Add("VideoId", videoId);
    using var streamContent = new StreamContent(video.OpenReadStream());
    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(video.ContentType);
    
    using var storageRequest = new HttpRequestMessage(HttpMethod.Post, videoStorageUrl);
    storageRequest.Headers.Add("VideoId", videoId);
    storageRequest.Content = streamContent;

    var storageResponse = await client.SendAsync(storageRequest);
    if (!storageResponse.IsSuccessStatusCode)
    {
        tracker.Sagas.TryRemove(videoId, out _);
        return Results.StatusCode(500); // Fail fast
    }

    // Step 1b: Upload Thumbnail to Storage
    if (thumbnail != null && thumbnail.Length > 0)
    {
        var thumbnailUrl = "http://video-storage:8080/thumbnail";
        using var thumbStreamContent = new StreamContent(thumbnail.OpenReadStream());
        thumbStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(thumbnail.ContentType);

        using var thumbStorageRequest = new HttpRequestMessage(HttpMethod.Post, thumbnailUrl);
        thumbStorageRequest.Headers.Add("VideoId", videoId);
        thumbStorageRequest.Content = thumbStreamContent;

        var thumbResponse = await client.SendAsync(thumbStorageRequest);
        if (!thumbResponse.IsSuccessStatusCode)
        {
            app.Logger.LogWarning($"Failed to upload thumbnail for video {videoId}");
            // Non-fatal, continuing saga
        }
    }

    // Step 2: Publish Events to independent queues
    try
    {
        using var channel = await rabbitConnection.CreateChannelAsync();
        
        var eventMessage = new { VideoId = videoId, Title = title, Description = description, PublisherId = publisherId ?? "AnonymousUser" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage));

        // 1. Metadata Queue
        await channel.QueueDeclareAsync(queue: "video_metadata", durable: true, exclusive: false, autoDelete: false, arguments: null);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "video_metadata", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: body);

        // 2. Views Initialization Queue
        await channel.QueueDeclareAsync(queue: "views", durable: true, exclusive: false, autoDelete: false, arguments: null);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "views", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: body);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to publish VideoUploadedEvent to RabbitMQ");
        tracker.Sagas.TryRemove(videoId, out _);
        // Rollback storage immediately if MQ goes down
        using var rollbackRequest = new HttpRequestMessage(HttpMethod.Delete, "http://video-storage:8080/video");
        rollbackRequest.Headers.Add("videoId", videoId);
        await client.SendAsync(rollbackRequest);
        return Results.StatusCode(500);
    }

    return Results.Ok(new { VideoId = videoId, Status = "Upload initiated. Processing via Saga.", Title = title });
}).DisableAntiforgery(); // Disable AntiForgery for API form upload

app.Run();

public class SagaState
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PublisherId { get; set; } = "AnonymousUser";
    public bool MetadataSuccess { get; set; }
    public bool ViewsSuccess { get; set; }
    public bool IsFailed { get; set; }
}

public class SagaTracker
{
    public ConcurrentDictionary<string, SagaState> Sagas { get; } = new();
}

public class UploadWorkflowOrchestrator : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SagaTracker _tracker;
    private readonly ILogger<UploadWorkflowOrchestrator> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public UploadWorkflowOrchestrator(IConfiguration configuration, IHttpClientFactory httpClientFactory, SagaTracker tracker, ILogger<UploadWorkflowOrchestrator> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _tracker = tracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitHostname = _configuration.GetConnectionString("RabbitMq") ?? "rabbitmq";
        var factory = new ConnectionFactory { HostName = rabbitHostname };
        
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(queue: "upload_replies", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue: "video_uploaded", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue: "storage_rollback", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                var reply = JsonSerializer.Deserialize<JsonElement>(message);
                if (reply.TryGetProperty("VideoId", out var vIdProp) && 
                    reply.TryGetProperty("Service", out var serviceProp) && 
                    reply.TryGetProperty("Status", out var statusProp))
                {
                    var videoId = vIdProp.GetString();
                    var service = serviceProp.GetString();
                    var status = statusProp.GetString();

                    if (!string.IsNullOrEmpty(videoId) && _tracker.Sagas.TryGetValue(videoId, out var saga) && !saga.IsFailed)
                    {
                        if (status == "Error")
                        {
                            saga.IsFailed = true;
                            _logger.LogError($"Upload failed for video {videoId} at service {service}. Initiating rollback.");
                            
                            // Rollback storage via HTTP
                            try
                            {
                                using var client = _httpClientFactory.CreateClient();
                                using var rollbackRequest = new HttpRequestMessage(HttpMethod.Delete, "http://video-storage:8080/video");
                                rollbackRequest.Headers.Add("videoId", videoId);
                                await client.SendAsync(rollbackRequest, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "HTTP rollback to storage failed.");
                            }

                            // Publish storage_rollback for any other listeners
                            var rollbackBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { VideoId = videoId }));
                            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "storage_rollback", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: rollbackBytes, cancellationToken: stoppingToken);
                            
                            _tracker.Sagas.TryRemove(videoId, out _);
                        }
                        else if (status == "Success")
                        {
                            if (service == "Metadata") saga.MetadataSuccess = true;
                            if (service == "Views") saga.ViewsSuccess = true;

                            // Check if fully successful
                            if (saga.MetadataSuccess && saga.ViewsSuccess)
                            {
                                _logger.LogInformation($"Video {videoId} completed saga! Publishing video_uploaded event.");
                                var successEvent = new { VideoId = videoId, Title = saga.Title, PublisherId = saga.PublisherId };
                                var successBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(successEvent));
                                
                                // Step 5: Publish successful upload event
                                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: "video_uploaded", mandatory: false, basicProperties: new RabbitMQ.Client.BasicProperties(), body: successBytes, cancellationToken: stoppingToken);
                                
                                _tracker.Sagas.TryRemove(videoId, out _);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing upload reply");
            }
            finally
            {
                if (_channel is not null)
                {
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
            }
        };

        // Listen for replies in the Upload Orchestrator
        await _channel.BasicConsumeAsync(queue: "upload_replies", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken: stoppingToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken: stoppingToken);
        await base.StopAsync(stoppingToken);
    }
}
