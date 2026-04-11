using BlogService.Data;
using BlogService.DTOs;
using BlogService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogService.Controllers;

[ApiController]
[Route("api/blogs")]
public class BlogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BlogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BlogResponseDto>>> GetAll()
    {
        var blogs = await _context.Blogs
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => ToResponse(b))
            .ToListAsync();
        return Ok(blogs); 
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogResponseDto>> GetById(int id)
    {
        var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);
        if (blog == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(blog));
    }

    [HttpPost]
    public async Task<ActionResult<BlogResponseDto>> Create([FromBody] CreateBlogDto dto)
    {
        var invalidImage = dto.ImageUrls?.FirstOrDefault(url =>
            !Uri.IsWellFormedUriString(url, UriKind.Absolute));

        if (invalidImage is not null)
        {
            return BadRequest("All image URLs must be absolute URLs.");
        }

        var blog = new Blog
        {
            Title = dto.Title.Trim(),
            DescriptionMarkdown = dto.DescriptionMarkdown,
            CreatedAtUtc = DateTime.UtcNow,
            ImageUrls = dto.ImageUrls
        };

        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = blog.Id }, ToResponse(blog));
    }

    private static BlogResponseDto ToResponse(Blog blog)
    {
        return new BlogResponseDto
        {
            Id = blog.Id,
            Title = blog.Title,
            DescriptionMarkdown = blog.DescriptionMarkdown,
            CreatedAtUtc = blog.CreatedAtUtc,
            ImageUrls = blog.ImageUrls
        };
    }
}
