using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using gateway.DTO;

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
builder.Services.AddHttpClient("IdentityService", (sp, client) => 
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrls>>().Value;
    client.BaseAddress = new Uri(urls.IdentityService);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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

// 1. GET /video
app.MapGet("/video", async ([FromHeader] string videoId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoStreaming");
    var request = new HttpRequestMessage(HttpMethod.Get, "/video");
    request.Headers.Add("videoId", videoId);
    
    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    if (!response.IsSuccessStatusCode) return Results.StatusCode((int)response.StatusCode);
    
    var stream = await response.Content.ReadAsStreamAsync();
    return Results.Stream(stream, response.Content.Headers.ContentType?.ToString() ?? "video/mp4");
});

// 2. GET /recent-videos
app.MapGet("/recent-videos", async ([FromHeader] string? videoId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoStreaming");
    var request = new HttpRequestMessage(HttpMethod.Get, "/recent-videos");
    if (!string.IsNullOrEmpty(videoId))
        request.Headers.Add("videoId", videoId);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 3. GET /video-details
app.MapGet("/video-details", async ([FromHeader] string videoId, IHttpClientFactory clientFactory) =>
{
    var metadataClient = clientFactory.CreateClient("VideoMetadata");
    var viewsClient = clientFactory.CreateClient("VideoViews");

    var metaRequest = new HttpRequestMessage(HttpMethod.Get, $"/metadata?videoId={videoId}");
    metaRequest.Headers.Add("videoId", videoId);

    var viewsRequest = new HttpRequestMessage(HttpMethod.Get, $"/views?videoId={videoId}");
    viewsRequest.Headers.Add("videoId", videoId);

    var metaTask = metadataClient.SendAsync(metaRequest);
    var viewsTask = viewsClient.SendAsync(viewsRequest);

    await Task.WhenAll(metaTask, viewsTask);

    var metaResponse = await metaTask;
    var viewsResponse = await viewsTask;

    var metaContent = metaResponse.IsSuccessStatusCode ? await metaResponse.Content.ReadFromJsonAsync<object>() : null;
    var viewsContent = viewsResponse.IsSuccessStatusCode ? await viewsResponse.Content.ReadFromJsonAsync<object>() : null;

    return Results.Ok(new VideoDetailsDto {
        Metadata = metaContent,
        Views = viewsContent
    });
});

// 4. POST /upload
app.MapPost("/upload", async ([FromForm] string title, [FromForm] string? description, IFormFile video, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("VideoUpload");
    
    using var content = new MultipartFormDataContent();
    content.Add(new StringContent(title), "title");
    if (!string.IsNullOrEmpty(description))
    {
        content.Add(new StringContent(description), "description");
    }
    
    using var videoStream = video.OpenReadStream();
    var videoContent = new StreamContent(videoStream);
    videoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(video.ContentType);
    content.Add(videoContent, "video", video.FileName);

    var request = new HttpRequestMessage(HttpMethod.Post, "/upload")
    {
        Content = content
    };

    var response = await client.SendAsync(request);
    var responseContent = await response.Content.ReadAsStringAsync();
    return Results.Content(responseContent, response.Content.Headers.ContentType?.ToString() ?? "application/json", null, (int)response.StatusCode);
}).DisableAntiforgery();

// 5. POST /comment
app.MapPost("/comment", async (HttpRequest req, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Post, "/comment")
    {
        Content = new StreamContent(req.Body)
    };
    if (req.ContentType != null)
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(req.ContentType);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json", null, (int)response.StatusCode);
});

// 6. GET /comment
app.MapGet("/comment", async ([FromHeader] string videoId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Get, "/comment");
    request.Headers.Add("videoId", videoId);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// 7. GET /replies
app.MapGet("/replies", async ([FromHeader] string commentId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("CommentService");
    var request = new HttpRequestMessage(HttpMethod.Get, "/replies");
    request.Headers.Add("commentId", commentId);

    var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json", null, (int)response.StatusCode);
});

// Identity Service Forwarding Endpoints
var identityEndpoints = new[] { "/login", "/register", "/profile", "/update-profile", "/oauth-login", "/oauth-register" };

foreach (var endpoint in identityEndpoints)
{
    app.MapMethods(endpoint, new[] { "GET", "POST", "PUT", "DELETE" }, async (HttpContext context, IHttpClientFactory clientFactory) =>
    {
        var client = clientFactory.CreateClient("IdentityService");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), endpoint + context.Request.QueryString);
        
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
