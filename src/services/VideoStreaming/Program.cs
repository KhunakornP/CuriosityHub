using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// Register MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://mongodb:27017";
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("video_db").GetCollection<VideoCache>("videos"));

// Register RabbitMQ Connection as a Singleton so it stays connected and lists the publisher
builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory { HostName = "rabbitmq" });
builder.Services.AddSingleton<IConnection>(sp => 
{
    var factory = sp.GetRequiredService<IConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// Add Hosted Service to listen for cached videos
builder.Services.AddHostedService<VideoCacheListener>();

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

app.MapGet("/recent-videos", async (IMongoCollection<VideoCache> collection, IHttpClientFactory httpClientFactory) =>
{
    var recentVideos = await collection.Find(_ => true)
        .SortByDescending(v => v.Id)
        .Limit(12)
        .ToListAsync();
    
    // Fetch views for the recent videos
    var videoIds = recentVideos.Select(v => v.VideoId).ToArray();
    Dictionary<string, long> viewCounts = new();

    if (videoIds.Length > 0)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var queryParams = string.Join("&", videoIds.Select(id => $"videoIds={id}"));
            var response = await client.GetAsync($"http://video-views:8080/video-views?{queryParams}");
            
            if (response.IsSuccessStatusCode)
            {
                var viewsData = await response.Content.ReadFromJsonAsync<JsonElement[]>();
                if (viewsData != null)
                {
                    foreach (var item in viewsData)
                    {
                        var vId = item.GetProperty("videoId").GetString();
                        var count = item.GetProperty("totalViews").GetInt64();
                        if (!string.IsNullOrEmpty(vId))
                        {
                            viewCounts[vId] = count;
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to 0 views if the service is unreachable
        }
    }

    // Process Publishers
    var clientFactoryForIdentity = httpClientFactory.CreateClient();
    foreach (var video in recentVideos)
    {
        if ((string.IsNullOrEmpty(video.PublisherName) || video.PublisherName == "CuriosityHub User") 
            && !string.IsNullOrEmpty(video.PublisherId) && video.PublisherId != "AnonymousUser")
        {
            try
            {
                var identityResponse = await clientFactoryForIdentity.GetAsync($"http://identity-service:8080/public-profile?targetId={video.PublisherId}");
                if (identityResponse.IsSuccessStatusCode)
                {
                    var jsonDoc = await identityResponse.Content.ReadFromJsonAsync<JsonElement>();
                    var firstName = jsonDoc.TryGetProperty("firstName", out var f) ? f.GetString() : "";
                    var lastName = jsonDoc.TryGetProperty("lastName", out var l) ? l.GetString() : "";
                    var newName = $"{firstName} {lastName}".Trim();
                    
                    if (!string.IsNullOrEmpty(newName))
                    {
                        video.PublisherName = newName;
                        
                        var update = Builders<VideoCache>.Update.Set(v => v.PublisherName, newName);
                        await collection.UpdateOneAsync(v => v.VideoId == video.VideoId, update);
                    }
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }
    }

    // Map to ViewModel for rendering
    var dtos = recentVideos.Select(v => new {
        id = v.VideoId,
        title = v.Title,
        thumbnailUrl = v.ThumbnailUrl,
        channelName = string.IsNullOrEmpty(v.PublisherName) ? "CuriosityHub User" : v.PublisherName,
        views = viewCounts.TryGetValue(v.VideoId, out var vc) ? vc : 0,
        publishedAt = v.CachedAt.ToString("MMM dd, yyyy")
    });

    return Results.Ok(dtos);
});

app.MapGet("/user-videos/{userId}", async (string userId, int page, int pageSize, IMongoCollection<VideoCache> collection, IHttpClientFactory httpClientFactory) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 50) pageSize = 50;

    var skip = (page - 1) * pageSize;

    var filter = Builders<VideoCache>.Filter.Eq(v => v.PublisherId, userId);
    var totalCount = await collection.CountDocumentsAsync(filter);
    var userVideos = await collection.Find(filter)
        .SortByDescending(v => v.Id)
        .Skip(skip)
        .Limit(pageSize)
        .ToListAsync();

    var videoIds = userVideos.Select(v => v.VideoId).ToArray();
    Dictionary<string, long> viewCounts = new();

    if (videoIds.Length > 0)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var queryParams = string.Join("&", videoIds.Select(id => $"videoIds={id}"));
            var response = await client.GetAsync($"http://video-views:8080/video-views?{queryParams}");
            
            if (response.IsSuccessStatusCode)
            {
                var viewsData = await response.Content.ReadFromJsonAsync<JsonElement[]>();
                if (viewsData != null)
                {
                    foreach (var item in viewsData)
                    {
                        var vId = item.GetProperty("videoId").GetString();
                        var count = item.GetProperty("totalViews").GetInt64();
                        if (!string.IsNullOrEmpty(vId))
                        {
                            viewCounts[vId] = count;
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to 0
        }
    }

    var dtos = userVideos.Select(v => new {
        id = v.VideoId,
        title = v.Title,
        thumbnailUrl = v.ThumbnailUrl,
        channelName = string.IsNullOrEmpty(v.PublisherName) ? "CuriosityHub User" : v.PublisherName,
        views = viewCounts.TryGetValue(v.VideoId, out var vc) ? vc : 0,
        publishedAt = v.CachedAt.ToString("MMM dd, yyyy")
    });

    return Results.Ok(new
    {
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Videos = dtos
    });
});

app.Run();

public class VideoCache
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string VideoId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string PublisherId { get; set; } = string.Empty;
    public string PublisherName { get; set; } = "CuriosityHub User";
    public DateTime CachedAt { get; set; }
}

public class VideoCacheEvent
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PublisherId { get; set; } = string.Empty;
}

public class VideoCacheListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public VideoCacheListener(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Step 6: video streaming listens to the video upload (successful upload event)
        await channel.QueueDeclareAsync(queue: "video_uploaded", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var evt = JsonSerializer.Deserialize<VideoCacheEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (evt != null && !string.IsNullOrEmpty(evt.VideoId))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoCache>>();
                    
                    // Fetch thumbnail from video-storage
                    string thumbnailUrl = "";
                    try 
                    {
                        var client = _httpClientFactory.CreateClient();
                        var request = new HttpRequestMessage(HttpMethod.Get, "http://video-storage:8080/thumbnail");
                        request.Headers.Add("videoId", evt.VideoId);
                        
                        var response = await client.SendAsync(request, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync(stoppingToken);
                            var base64 = Convert.ToBase64String(bytes);
                            // Get content type
                            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                            thumbnailUrl = $"data:{contentType};base64,{base64}";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or ignore if thumbnail fetching fails
                    }

                    // Fetch Publisher name from identity-service
                    string publisherName = "CuriosityHub User";
                    if (!string.IsNullOrEmpty(evt.PublisherId) && evt.PublisherId != "AnonymousUser")
                    {
                        try
                        {
                            var client = _httpClientFactory.CreateClient();
                            var identityResponse = await client.GetAsync($"http://identity-service:8080/public-profile?targetId={evt.PublisherId}", stoppingToken);
                            if (identityResponse.IsSuccessStatusCode)
                            {
                                var jsonDoc = await identityResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: stoppingToken);
                                var firstName = jsonDoc.TryGetProperty("firstName", out var f) ? f.GetString() : "";
                                var lastName = jsonDoc.TryGetProperty("lastName", out var l) ? l.GetString() : "";
                                publisherName = $"{firstName} {lastName}".Trim();
                                if (string.IsNullOrEmpty(publisherName)) publisherName = "CuriosityHub User";
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue with default name if identity service fails
                        }
                    }

                    var entry = new VideoCache 
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        VideoId = evt.VideoId,
                        Title = evt.Title,
                        ThumbnailUrl = thumbnailUrl,
                        PublisherId = evt.PublisherId ?? "AnonymousUser",
                        PublisherName = publisherName,
                        CachedAt = DateTime.UtcNow
                    };
                    
                    // Upsert to be safe
                    await collection.ReplaceOneAsync(x => x.VideoId == evt.VideoId, entry, new ReplaceOptions { IsUpsert = true }, cancellationToken: stoppingToken);
                }
            }
            catch { /* Ignore parsing/insert errors for resilience */ }
        };

        await channel.BasicConsumeAsync(queue: "video_uploaded", autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
        
        // Wait until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
