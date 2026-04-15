using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Config bindings
builder.Services.Configure<ServiceUrls>(builder.Configuration.GetSection("ServiceUrls"));

// Add services to the container.
builder.Services.AddOpenApi();

// Register HttpClients for downstream services using IOptions
builder.Services.AddHttpClient("VideoStreaming", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.VideoStreaming);
});
builder.Services.AddHttpClient("VideoMetadata", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.VideoMetadata);
});
builder.Services.AddHttpClient("VideoViews", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.VideoViews);
});
builder.Services.AddHttpClient("VideoUpload", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.VideoUpload);
});
builder.Services.AddHttpClient("CommentService", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.CommentService);
});
builder.Services.AddHttpClient("VideoStorage", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.VideoStorage);
});
builder.Services.AddHttpClient("IdentityService", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.IdentityService);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Often needed for frontend frameworks
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseHttpsRedirection();

// 1. Delete video
app.MapDelete("/video", async ([FromQuery] string videoId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoUpload");
    var request = new HttpRequestMessage(HttpMethod.Delete, $"/delete?videoId={videoId}");

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 2. Update video metadata
app.MapPut("/video-metadata", async (HttpRequest req, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoMetadata");
    var request = new HttpRequestMessage(HttpMethod.Put, "/update")
    {
        Content = new StreamContent(req.Body)
    };
    if (req.ContentType != null)
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(req.ContentType);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json", null, (int)response.StatusCode);
});

// 3. Update comment
app.MapPut("/comment", async (HttpRequest req, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Put, "/update")
    {
        Content = new StreamContent(req.Body)
    };
    if (req.ContentType != null)
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(req.ContentType);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json", null, (int)response.StatusCode);
});

// 4. Delete comment
app.MapDelete("/comment", async ([FromQuery] string commentId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Delete, $"/delete?commentId={commentId}");

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 5. Get comments paginate
app.MapGet("/comments", async ([FromQuery] int page, [FromQuery] int pageSize, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var response = await client.GetAsync($"/all?page={page}&pageSize={pageSize}");
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 6. Get videos aggregate
app.MapGet("/videos", async ([FromQuery] int page, [FromQuery] int pageSize, IHttpClientFactory clientFactory) =>
{
    var storageClient = clientFactory.CreateClient("VideoStorage");
    var listResponse = await storageClient.GetAsync($"/video-list?page={page}&pageSize={pageSize}");
    
    if (!listResponse.IsSuccessStatusCode)
    {
         return Results.StatusCode((int)listResponse.StatusCode);
    }
    
    var videoIds = await listResponse.Content.ReadFromJsonAsync<string[]>();
    if (videoIds == null || !videoIds.Any())
    {
        return Results.Ok(new List<object>());
    }

    var metadataClient = clientFactory.CreateClient("VideoMetadata");
    var viewsClient = clientFactory.CreateClient("VideoViews");

    var queryParams = string.Join("&", videoIds.Select(id => $"videoIds={id}"));

    var metaTask = metadataClient.GetAsync($"/video-metadatas?{queryParams}");
    var viewsTask = viewsClient.GetAsync($"/video-views?{queryParams}");

    await Task.WhenAll(metaTask, viewsTask);

    var metaResponse = await metaTask;
    var viewsResponse = await viewsTask;

    var metaContent = metaResponse.IsSuccessStatusCode 
        ? await metaResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement[]>() ?? Array.Empty<System.Text.Json.JsonElement>()
        : Array.Empty<System.Text.Json.JsonElement>();

    var viewsContent = viewsResponse.IsSuccessStatusCode 
        ? await viewsResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement[]>() ?? Array.Empty<System.Text.Json.JsonElement>()
        : Array.Empty<System.Text.Json.JsonElement>();

    var results = videoIds.Select(id =>
    {
        var metadata = metaContent.FirstOrDefault(m => 
            (m.TryGetProperty("videoId", out var prop) && prop.GetString() == id) ||
            (m.TryGetProperty("VideoId", out var prop2) && prop2.GetString() == id));
            
        var views = viewsContent.FirstOrDefault(v => 
            (v.TryGetProperty("videoId", out var prop) && prop.GetString() == id) || 
            (v.TryGetProperty("VideoId", out var prop2) && prop2.GetString() == id));

        return new
        {
            videoId = id,
            title = metadata.ValueKind != System.Text.Json.JsonValueKind.Undefined && metadata.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "Unknown",
            description = metadata.ValueKind != System.Text.Json.JsonValueKind.Undefined && metadata.TryGetProperty("description", out var descProp) ? descProp.GetString() : "",
            views = views.ValueKind != System.Text.Json.JsonValueKind.Undefined && views.TryGetProperty("views", out var viewsProp) ? viewsProp.GetInt32() : 0,
            likes = views.ValueKind != System.Text.Json.JsonValueKind.Undefined && views.TryGetProperty("likes", out var likesProp) ? likesProp.GetInt32() : 0
        };
    });

    return Results.Ok(results);
});

// 7. Video Upload to VideoUpload (Admin)
app.MapPost("/upload", async (HttpRequest req, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoUpload");
    var request = new HttpRequestMessage(HttpMethod.Post, "/upload")
    {
        Content = new StreamContent(req.Body)
    };

    if (req.ContentType != null)
    {
        request.Content.Headers.TryAddWithoutValidation("Content-Type", req.ContentType);
    }
    
    // Copy the Authorization header logic if an admin token is passed
    if (req.Headers.ContainsKey("Authorization"))
    {
        request.Headers.TryAddWithoutValidation("Authorization", req.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 8. Create comment
app.MapPost("/comment", async (HttpRequest req, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Post, "/comment")
    {
        Content = new StreamContent(req.Body)
    };
    if (req.ContentType != null)
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(req.ContentType);
        
    if (req.Headers.ContainsKey("Authorization"))
        request.Headers.TryAddWithoutValidation("Authorization", req.Headers["Authorization"].ToString());

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json", null, (int)response.StatusCode);
});

// Identity Service Forwarding Endpoints
var identityEndpoints = new[] { "/login", "/register", "/profile", "/update-profile", "/oauth-login", "/oauth-register", "/admin/users", "/admin/user", "/admin/user/{id}" };

foreach (var endpoint in identityEndpoints)
{
    app.MapMethods(endpoint, new[] { "GET", "POST", "PUT", "DELETE" }, async (HttpContext context, IHttpClientFactory clientFactory) =>
    {
        var client = clientFactory.CreateClient("IdentityService");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path.Value + context.Request.QueryString);
        
        if (context.Request.ContentLength > 0 || context.Request.Headers.TransferEncoding.Count > 0)
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.ToLower() != "host" && header.Key.ToLower() != "content-type" && header.Key.ToLower() != "content-length")
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        var content = await response.Content.ReadAsByteArrayAsync();
        context.Response.StatusCode = (int)response.StatusCode;
        return Results.Bytes(content, response.Content.Headers.ContentType?.ToString());
    }).DisableAntiforgery();
}

app.Run();

public class ServiceUrls
{
    public string VideoStreaming { get; set; } = "http://localhost:5001";
    public string VideoMetadata { get; set; } = "http://localhost:5002";
    public string VideoViews { get; set; } = "http://localhost:5003";
    public string VideoUpload { get; set; } = "http://localhost:5004";
    public string CommentService { get; set; } = "http://localhost:5005";
    public string VideoStorage { get; set; } = "http://localhost:8080";
    public string IdentityService { get; set; } = "http://localhost:8086";
}
