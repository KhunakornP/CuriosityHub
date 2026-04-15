namespace CommentService.Models;

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
