namespace CommentService.Models.Dtos;

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