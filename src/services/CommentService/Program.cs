using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=mariadb;Database=commentsdb;User=root;Password=secret;";

builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Auto-migrate on startup for development convenience
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/comment", async ([FromBody] CreateCommentRequest request, CommentDbContext db) =>
{
    var comment = new Comment
    {
        Id = Guid.NewGuid().ToString(),
        UserId = request.User,
        Text = request.Comment,
        ParentCommentId = request.Parent_Comment,
        VideoId = request.Video_Id,
        CreatedAt = DateTime.UtcNow,
        Nested = false
    };

    db.Comments.Add(comment);

    if (!string.IsNullOrEmpty(request.Parent_Comment))
    {
        var parent = await db.Comments.FindAsync(request.Parent_Comment);
        if (parent != null)
        {
            parent.Nested = true;
        }
    }

    await db.SaveChangesAsync();
    return Results.Created($"/comment/{comment.Id}", comment);
});

app.MapGet("/comment", async ([FromHeader] string videoId, CommentDbContext db) =>
{
    if (string.IsNullOrEmpty(videoId))
        return Results.BadRequest("videoId header is required.");

    var comments = await db.Comments
        .Where(c => c.VideoId == videoId && string.IsNullOrEmpty(c.ParentCommentId))
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();
        
    return Results.Ok(comments);
});

app.MapGet("/replies", async ([FromHeader] string commentId, CommentDbContext db) =>
{
    if (string.IsNullOrEmpty(commentId))
        return Results.BadRequest("commentId header is required.");

    var replies = await db.Comments
        .Where(c => c.ParentCommentId == commentId)
        .OrderBy(c => c.CreatedAt)
        .ToListAsync();
        
    return Results.Ok(replies);
});

app.MapPut("/update", async ([FromBody] UpdateCommentRequest request, CommentDbContext db) =>
{
    if (string.IsNullOrEmpty(request.Id))
        return Results.BadRequest("Comment Id is required.");
        
    var comment = await db.Comments.FindAsync(request.Id);
    if (comment == null) 
        return Results.NotFound("Comment not found.");
    
    comment.Text = request.Text;
    await db.SaveChangesAsync();
    
    return Results.Ok(new { message = "Comment updated successfully." });
});

app.MapDelete("/delete", async ([FromQuery] string commentId, CommentDbContext db) =>
{
    if (string.IsNullOrEmpty(commentId))
        return Results.BadRequest("Comment Id is required.");

    var comment = await db.Comments.FindAsync(commentId);
    if (comment == null)
        return Results.NotFound("Comment not found.");

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Comment deleted successfully. All replies gracefully cascaded." });
});

app.MapGet("/all", async ([FromQuery] int page, [FromQuery] int pageSize, CommentDbContext db) => 
{
    int safePage = page > 0 ? page : 1;
    int safePageSize = pageSize > 0 ? pageSize : 10;
    
    var comments = await db.Comments
        .OrderByDescending(c => c.CreatedAt)
        .Skip((safePage - 1) * safePageSize)
        .Take(safePageSize)
        .ToListAsync();
        
    return Results.Ok(comments);
});

app.Run();

public class CommentDbContext : DbContext
{
    public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options) { }

    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>()
            .HasOne<Comment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade); // The magic for cascading deletes down the tree
    }
}

public class Comment
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ParentCommentId { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Nested { get; set; } = false;
}

public class CreateCommentRequest
{
    public string User { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string? Parent_Comment { get; set; }
    public string Video_Id { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
