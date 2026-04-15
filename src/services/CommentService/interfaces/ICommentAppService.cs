using CommentService.Models;
using CommentService.Models.Dtos;

namespace CommentService.Interfaces;

public interface ICommentAppService
{
    Task<Comment> CreateCommentAsync(CreateCommentRequest request);
    Task<IEnumerable<Comment>> GetCommentsByVideoIdAsync(string videoId);
    Task<IEnumerable<Comment>> GetRepliesAsync(string commentId);
    Task<bool> UpdateCommentAsync(UpdateCommentRequest request);
    Task<bool> DeleteCommentAsync(string commentId);
    Task<IEnumerable<Comment>> GetAllCommentsAsync(int page, int pageSize);
}