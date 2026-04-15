using CommentService.Data;
using CommentService.Interfaces;
using CommentService.Models;
using CommentService.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Services;

public class CommentAppService : ICommentAppService
{
    private readonly CommentDbContext _db;

    public CommentAppService(CommentDbContext db)
    {
        _db = db;
    }

    public async Task<Comment> CreateCommentAsync(CreateCommentRequest request)
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

        _db.Comments.Add(comment);

        if (!string.IsNullOrEmpty(request.Parent_Comment))
        {
            var parent = await _db.Comments.FindAsync(request.Parent_Comment);
            if (parent != null)
            {
                parent.Nested = true;
            }
        }

        await _db.SaveChangesAsync();
        return comment;
    }

    public async Task<IEnumerable<Comment>> GetCommentsByVideoIdAsync(string videoId)
    {
        return await _db.Comments
            .Where(c => c.VideoId == videoId && string.IsNullOrEmpty(c.ParentCommentId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetRepliesAsync(string commentId)
    {
        return await _db.Comments
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateCommentAsync(UpdateCommentRequest request)
    {
        var comment = await _db.Comments.FindAsync(request.Id);
        if (comment == null) 
            return false;
        
        comment.Text = request.Text;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(string commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null)
            return false;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Comment>> GetAllCommentsAsync(int page, int pageSize)
    {
        int safePage = page > 0 ? page : 1;
        int safePageSize = pageSize > 0 ? pageSize : 10;
        
        return await _db.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();
    }
}