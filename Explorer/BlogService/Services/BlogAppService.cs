using BlogService.DTOs;
using BlogService.Models;
using BlogService.Repositories;

namespace BlogService.Services;

public class BlogAppService : IBlogService
{
    private readonly IBlogRepository _repository;

    public BlogAppService(IBlogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BlogResponseDto>> GetAllAsync()
    {
        var blogs = await _repository.GetAllWithDetailsAsync();
        return blogs.Select(ToBlogResponse).ToList();
    }

    public async Task<BlogResponseDto?> GetByIdAsync(int blogId)
    {
        var blog = await _repository.GetByIdWithDetailsAsync(blogId);
        return blog is null ? null : ToBlogResponse(blog);
    }

    public async Task<BlogResponseDto> CreateAsync(CreateBlogDto dto, CurrentUser currentUser)
    {
        var invalidImage = dto.ImageUrls?.FirstOrDefault(url =>
            !Uri.IsWellFormedUriString(url, UriKind.Absolute));

        if (invalidImage is not null)
        {
            throw new ArgumentException("All image URLs must be absolute URLs.");
        }

        var blog = new Blog
        {
            Title = dto.Title.Trim(),
            DescriptionMarkdown = dto.DescriptionMarkdown,
            AuthorId = currentUser.UserId,
            AuthorUsername = currentUser.Username,
            CreatedAtUtc = DateTime.UtcNow,
            ImageUrls = dto.ImageUrls
        };

        await _repository.AddBlogAsync(blog);
        await _repository.SaveChangesAsync();

        return ToBlogResponse(blog);
    }

    private static BlogResponseDto ToBlogResponse(Blog blog)
    {
        return new BlogResponseDto
        {
            Id = blog.Id,
            Title = blog.Title,
            DescriptionMarkdown = blog.DescriptionMarkdown,
            AuthorId = blog.AuthorId,
            AuthorUsername = blog.AuthorUsername,
            CreatedAtUtc = blog.CreatedAtUtc,
            ImageUrls = blog.ImageUrls,
            LikesCount = blog.Likes.Count,
            Comments = blog.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(ToCommentResponse)
                .ToList()
        };
    }

    private static CommentResponseDto ToCommentResponse(BlogComment comment)
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
