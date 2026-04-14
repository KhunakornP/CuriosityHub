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

app.Run();

public class ServiceUrls
{
    public string VideoStreaming { get; set; } = "http://localhost:5001";
    public string VideoMetadata { get; set; } = "http://localhost:5002";
    public string VideoViews { get; set; } = "http://localhost:5003";
    public string VideoUpload { get; set; } = "http://localhost:5004";
    public string CommentService { get; set; } = "http://localhost:5005";
}
