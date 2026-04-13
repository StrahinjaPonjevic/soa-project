using BlogService.DTOs;
using BlogService.Models;
using BlogService.Repositories;

namespace BlogService.Services;

public class CommentService : ICommentService
{
    private readonly IBlogRepository _repository;

    public CommentService(IBlogRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommentResponseDto> AddCommentAsync(int blogId, CreateCommentDto dto, CurrentUser currentUser)
    {
        var blog = await _repository.GetByIdAsync(blogId);
        if (blog is null)
        {
            throw new KeyNotFoundException("Blog not found.");
        }

        var now = DateTime.UtcNow;
        var comment = new BlogComment
        {
            BlogId = blogId,
            AuthorId = currentUser.UserId,
            AuthorUsername = currentUser.Username,
            Text = dto.Text.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repository.AddCommentAsync(comment);
        await _repository.SaveChangesAsync();

        return ToResponse(comment);
    }

    public async Task<CommentResponseDto> UpdateCommentAsync(int blogId, int commentId, UpdateCommentDto dto, CurrentUser currentUser)
    {
        var blog = await _repository.GetByIdAsync(blogId);
        if (blog is null)
        {
            throw new KeyNotFoundException("Blog not found.");
        }

        var comment = await _repository.GetCommentAsync(blogId, commentId);
        if (comment is null)
        {
            throw new KeyNotFoundException("Comment not found.");
        }

        if (comment.AuthorId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only edit your own comments.");
        }

        comment.Text = dto.Text.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        return ToResponse(comment);
    }

    private static CommentResponseDto ToResponse(BlogComment comment)
    {
        return new CommentResponseDto
        {
            Id = comment.Id,
            BlogId = comment.BlogId,
            AuthorId = comment.AuthorId,
            AuthorUsername = comment.AuthorUsername,
            Text = comment.Text,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc
        };
    }
}
