using BlogService.Models;

namespace BlogService.Repositories;

public interface IBlogRepository
{
    Task<List<Blog>> GetAllWithDetailsAsync();
    Task<Blog?> GetByIdWithDetailsAsync(int blogId);
    Task<Blog?> GetByIdAsync(int blogId);
    Task AddBlogAsync(Blog blog);
    Task AddCommentAsync(BlogComment comment);
    Task<BlogComment?> GetCommentAsync(int blogId, int commentId);
    Task<BlogLike?> GetLikeAsync(int blogId, int userId);
    Task AddLikeAsync(BlogLike like);
    void RemoveLike(BlogLike like);
    Task SaveChangesAsync();
}
