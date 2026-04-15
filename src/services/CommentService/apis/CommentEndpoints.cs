using CommentService.Interfaces;
using CommentService.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Apis;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/comment", async ([FromBody] CreateCommentRequest request, ICommentAppService commentService) =>
        {
            var comment = await commentService.CreateCommentAsync(request);
            return Results.Created($"/comment/{comment.Id}", comment);
        });

        app.MapGet("/comment", async ([FromHeader] string videoId, ICommentAppService commentService) =>
        {
            if (string.IsNullOrEmpty(videoId))
                return Results.BadRequest("videoId header is required.");

            var comments = await commentService.GetCommentsByVideoIdAsync(videoId);
            return Results.Ok(comments);
        });

        app.MapGet("/replies", async ([FromHeader] string commentId, ICommentAppService commentService) =>
        {
            if (string.IsNullOrEmpty(commentId))
                return Results.BadRequest("commentId header is required.");

            var replies = await commentService.GetRepliesAsync(commentId);
            return Results.Ok(replies);
        });

        app.MapPut("/update", async ([FromBody] UpdateCommentRequest request, ICommentAppService commentService) =>
        {
            if (string.IsNullOrEmpty(request.Id))
                return Results.BadRequest("Comment Id is required.");
                
            var success = await commentService.UpdateCommentAsync(request);
            if (!success) 
                return Results.NotFound("Comment not found.");
            
            return Results.Ok(new { message = "Comment updated successfully." });
        });

        app.MapDelete("/delete", async ([FromQuery] string commentId, ICommentAppService commentService) =>
        {
            if (string.IsNullOrEmpty(commentId))
                return Results.BadRequest("Comment Id is required.");

            var success = await commentService.DeleteCommentAsync(commentId);
            if (!success)
                return Results.NotFound("Comment not found.");

            return Results.Ok(new { message = "Comment deleted successfully. All replies gracefully cascaded." });
        });

        app.MapGet("/all", async ([FromQuery] int page, [FromQuery] int pageSize, ICommentAppService commentService) => 
        {
            var comments = await commentService.GetAllCommentsAsync(page, pageSize);
            return Results.Ok(comments);
        });
    }
}