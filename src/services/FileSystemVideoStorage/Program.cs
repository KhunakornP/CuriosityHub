using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), "videos");
if (!Directory.Exists(videoDirectory))
{
    Directory.CreateDirectory(videoDirectory);
}

string thumbnailDirectory = Path.Combine(Directory.GetCurrentDirectory(), "thumbnails");
if (!Directory.Exists(thumbnailDirectory))
{
    Directory.CreateDirectory(thumbnailDirectory);
}

app.MapGet("/video", async (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(videoDirectory, $"{videoId}.mp4");

    if (!File.Exists(filePath))
    {
        return Results.NotFound("Video not found");
    }

    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return Results.File(fileStream, "video/mp4", enableRangeProcessing: true);
});

app.MapPost("/upload", async (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("VideoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid VideoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(videoDirectory, $"{videoId}.mp4");

    if (File.Exists(filePath))
    {
        return Results.Conflict("Video with this VideoId already exists.");
    }

    try
    {
        using var fileStream = new FileStream(filePath, FileMode.CreateNew);
        await context.Request.Body.CopyToAsync(fileStream);
        return Results.Ok($"Video {videoId} sucessfully saved.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Upload failed.");
        Console.Error.WriteLine(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPut("/video", async (HttpContext context) =>
{
    // 1. Extract and validate Header
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(videoDirectory, $"{videoId}.mp4");

    // 2. Strict existence check (The core requirement for PUT)
    if (!File.Exists(filePath))
    {
        return Results.NotFound($"Video with ID {videoId} does not exist. Use POST to create it.");
    }

    try
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await context.Request.Body.CopyToAsync(fileStream);
        return Results.Ok($"Video {videoId} sucessfully updated.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Upload failed.");
        Console.Error.WriteLine(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPost("/thumbnail", async (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(thumbnailDirectory, $"{videoId}.jpg");

    if (File.Exists(filePath))
    {
        return Results.Conflict("Thumbnail with this VideoId already exists.");
    }

    try
    {
        using var fileStream = new FileStream(filePath, FileMode.CreateNew);
        await context.Request.Body.CopyToAsync(fileStream);
        return Results.Ok($"Thumbnail {videoId} successfully saved.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Thumbnail upload failed.");
        Console.Error.WriteLine(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapPut("/thumbnail", async (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(thumbnailDirectory, $"{videoId}.jpg");

    if (!File.Exists(filePath))
    {
        return Results.NotFound($"Thumbnail with ID {videoId} does not exist. Use POST to create it.");
    }

    try
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await context.Request.Body.CopyToAsync(fileStream);
        return Results.Ok($"Thumbnail {videoId} successfully updated.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Thumbnail update failed.");
        Console.Error.WriteLine(ex.ToString());
        return Results.StatusCode(500);
    }
});

app.MapGet("/thumbnail", async (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(thumbnailDirectory, $"{videoId}.jpg");

    if (!File.Exists(filePath))
    {
        return Results.NotFound("Thumbnail not found");
    }

    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return Results.File(fileStream, "image/jpeg");
});


app.MapDelete("/video", (HttpContext context) =>
{
    if (!context.Request.Headers.TryGetValue("videoId", out var videoIdStr) || string.IsNullOrWhiteSpace(videoIdStr))
    {
        return Results.BadRequest("Missing or invalid videoId header");
    }

    var videoId = videoIdStr.ToString();
    var filePath = Path.Combine(videoDirectory, $"{videoId}.mp4");
    var thumbnailPath = Path.Combine(thumbnailDirectory, $"{videoId}.jpg");

    if (!File.Exists(filePath))
    {
        return Results.NotFound("Video not found");
    }

    File.Delete(filePath);

    if (File.Exists(thumbnailPath))
    {
        File.Delete(thumbnailPath);
    }

    return Results.Ok(new MessageResponse { Message = "Video and thumbnails deleted successfully" });
});

app.MapGet("/video-list", ([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
{
    var files = Directory.GetFiles(videoDirectory, "*.mp4")
                         .Select(Path.GetFileNameWithoutExtension)
                         .Where(name => !string.IsNullOrEmpty(name))
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();
    return Results.Ok(files);
});

app.Run();

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(MessageResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}
