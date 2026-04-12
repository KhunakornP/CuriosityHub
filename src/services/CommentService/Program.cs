using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure MongoDB client
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase("CuriosityHub");
builder.Services.AddSingleton(database);
builder.Services.AddSingleton(database.GetCollection<Comment>("Comments"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/comment", async ([FromBody] CreateCommentRequest request, [FromServices] IMongoCollection<Comment> commentsCollection) =>
{
    var comment = new Comment
    {
        Id = ObjectId.GenerateNewId().ToString(),
        UserId = request.User,
        Text = request.Comment,
        ParentCommentId = request.Parent_Comment,
        VideoId = request.Video_Id,
        CreatedAt = DateTime.UtcNow
    };

    await commentsCollection.InsertOneAsync(comment);
    return Results.Created($"/comment/{comment.Id}", comment);
});

app.MapGet("/comment", async ([FromHeader] string videoId, [FromServices] IMongoCollection<Comment> commentsCollection) =>
{
    if (string.IsNullOrEmpty(videoId))
        return Results.BadRequest("videoId header is required.");

    var filter = Builders<Comment>.Filter.Eq(c => c.VideoId, videoId) &
                 Builders<Comment>.Filter.Eq(c => c.ParentCommentId, null as string);
    
    var comments = await commentsCollection.Find(filter).SortByDescending(c => c.CreatedAt).ToListAsync();
    return Results.Ok(comments);
});

app.MapGet("/replies", async ([FromHeader] string commentId, [FromServices] IMongoCollection<Comment> commentsCollection) =>
{
    if (string.IsNullOrEmpty(commentId))
        return Results.BadRequest("commentId header is required.");

    var filter = Builders<Comment>.Filter.Eq(c => c.ParentCommentId, commentId);
    
    var replies = await commentsCollection.Find(filter).SortBy(c => c.CreatedAt).ToListAsync();
    return Results.Ok(replies);
});

app.Run();

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    
    public string Text { get; set; } = string.Empty;
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentCommentId { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string VideoId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentRequest
{
    public string User { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string? Parent_Comment { get; set; }
    public string Video_Id { get; set; } = string.Empty;
}
