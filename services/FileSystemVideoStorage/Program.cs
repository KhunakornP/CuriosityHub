using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

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


app.MapDelete("/video", (HttpContext context) =>
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

    File.Delete(filePath);

    return Results.Ok(new { Message = "Video deleted successfully" });
});

app.Run();
