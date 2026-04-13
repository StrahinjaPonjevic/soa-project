using BlogService.DTOs;
using BlogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogService.Controllers;

[ApiController]
[Route("api/blogs")]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly ICommentService _commentService;
    private readonly ILikeService _likeService;
    private readonly ICurrentUserService _currentUserService;

    public BlogsController(
        IBlogService blogService,
        ICommentService commentService,
        ILikeService likeService,
        ICurrentUserService currentUserService)
    {
        _blogService = blogService;
        _commentService = commentService;
        _likeService = likeService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BlogResponseDto>>> GetAll()
    {
        var blogs = await _blogService.GetAllAsync();
        return Ok(blogs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogResponseDto>> GetById(int id)
    {
        var blog = await _blogService.GetByIdAsync(id);
        if (blog == null)
        {
            return NotFound();
        }

        return Ok(blog);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BlogResponseDto>> Create([FromBody] CreateBlogDto dto)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var created = await _blogService.CreateAsync(dto, currentUser!);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{blogId:int}/comments")]
    public async Task<ActionResult<CommentResponseDto>> AddComment(int blogId, [FromBody] CreateCommentDto dto)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var comment = await _commentService.AddCommentAsync(blogId, dto, currentUser!);
            return CreatedAtAction(nameof(GetById), new { id = blogId }, comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{blogId:int}/comments/{commentId:int}")]
    public async Task<ActionResult<CommentResponseDto>> UpdateComment(
        int blogId,
        int commentId,
        [FromBody] UpdateCommentDto dto)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var comment = await _commentService.UpdateCommentAsync(blogId, commentId, dto, currentUser!);
            return Ok(comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPost("{blogId:int}/likes")]
    public async Task<IActionResult> AddLike(int blogId)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            await _likeService.AddLikeAsync(blogId, currentUser!);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{blogId:int}/likes")]
    public async Task<IActionResult> RemoveLike(int blogId)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            await _likeService.RemoveLikeAsync(blogId, currentUser!);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
