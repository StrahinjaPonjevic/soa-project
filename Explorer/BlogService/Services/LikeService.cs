using BlogService.Models;
using BlogService.Repositories;

namespace BlogService.Services;

public class LikeService : ILikeService
{
    private readonly IBlogRepository _repository;

    public LikeService(IBlogRepository repository)
    {
        _repository = repository;
    }

    public async Task AddLikeAsync(int blogId, CurrentUser currentUser)
    {
        var blog = await _repository.GetByIdAsync(blogId);
        if (blog is null)
        {
            throw new KeyNotFoundException("Blog not found.");
        }

        var existingLike = await _repository.GetLikeAsync(blogId, currentUser.UserId);
        if (existingLike is not null)
        {
            return;
        }

        var like = new BlogLike
        {
            BlogId = blogId,
            UserId = currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddLikeAsync(like);
        await _repository.SaveChangesAsync();
    }

    public async Task RemoveLikeAsync(int blogId, CurrentUser currentUser)
    {
        var blog = await _repository.GetByIdAsync(blogId);
        if (blog is null)
        {
            throw new KeyNotFoundException("Blog not found.");
        }

        var existingLike = await _repository.GetLikeAsync(blogId, currentUser.UserId);
        if (existingLike is null)
        {
            return;
        }

        _repository.RemoveLike(existingLike);
        await _repository.SaveChangesAsync();
    }
}
