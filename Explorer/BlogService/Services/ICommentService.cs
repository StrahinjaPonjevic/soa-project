using BlogService.DTOs;

namespace BlogService.Services;

public interface ICommentService
{
    Task<CommentResponseDto> AddCommentAsync(int blogId, CreateCommentDto dto, CurrentUser currentUser);
    Task<CommentResponseDto> UpdateCommentAsync(int blogId, int commentId, UpdateCommentDto dto, CurrentUser currentUser);
}
