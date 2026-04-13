using BlogService.Data;
using BlogService.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogService.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly AppDbContext _context;

    public BlogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Blog>> GetAllWithDetailsAsync()
    {
        return await _context.Blogs
            .AsNoTracking()
            .Include(b => b.Comments)
            .Include(b => b.Likes)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Blog?> GetByIdWithDetailsAsync(int blogId)
    {
        return await _context.Blogs
            .AsNoTracking()
            .Include(b => b.Comments)
            .Include(b => b.Likes)
            .FirstOrDefaultAsync(b => b.Id == blogId);
    }

    public Task<Blog?> GetByIdAsync(int blogId)
    {
        return _context.Blogs.FirstOrDefaultAsync(b => b.Id == blogId);
    }

    public async Task AddBlogAsync(Blog blog)
    {
        await _context.Blogs.AddAsync(blog);
    }

    public async Task AddCommentAsync(BlogComment comment)
    {
        await _context.BlogComments.AddAsync(comment);
    }

    public Task<BlogComment?> GetCommentAsync(int blogId, int commentId)
    {
        return _context.BlogComments
            .FirstOrDefaultAsync(c => c.BlogId == blogId && c.Id == commentId);
    }

    public Task<BlogLike?> GetLikeAsync(int blogId, int userId)
    {
        return _context.BlogLikes
            .FirstOrDefaultAsync(l => l.BlogId == blogId && l.UserId == userId);
    }

    public async Task AddLikeAsync(BlogLike like)
    {
        await _context.BlogLikes.AddAsync(like);
    }

    public void RemoveLike(BlogLike like)
    {
        _context.BlogLikes.Remove(like);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
