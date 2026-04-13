using BlogService.DTOs;

namespace BlogService.Services;

public interface IBlogService
{
    Task<IReadOnlyList<BlogResponseDto>> GetAllAsync();
    Task<BlogResponseDto?> GetByIdAsync(int blogId);
    Task<BlogResponseDto> CreateAsync(CreateBlogDto dto, CurrentUser currentUser);
}
