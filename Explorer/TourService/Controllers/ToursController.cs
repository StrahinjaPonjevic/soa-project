using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourService.Data;
using TourService.DTOs;
using TourService.Models;
using TourService.Services;

namespace TourService.Controllers;

[ApiController]
[Route("api/tours")]
public class ToursController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ToursController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TourResponseDto>> Create([FromBody] CreateTourDto dto)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var normalizedTags = dto.Tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        var tour = new Tour
        {
            AuthorId = currentUser!.UserId,
            AuthorUsername = currentUser.Username,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Difficulty = dto.Difficulty.Trim(),
            Tags = normalizedTags,
            Status = TourStatus.Draft,
            Price = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Tours.AddAsync(tour);
        await _context.SaveChangesAsync();

        var response = ToResponse(tour);
        return CreatedAtAction(nameof(GetById), new { id = tour.Id }, response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<TourResponseDto>>> GetMine()
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var tours = await _context.Tours
            .AsNoTracking()
            .Where(t => t.AuthorId == currentUser!.UserId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => ToResponse(t))
            .ToListAsync();

        return Ok(tours);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TourResponseDto>> GetById(int id)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tour is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(tour));
    }

    private static TourResponseDto ToResponse(Tour tour)
    {
        return new TourResponseDto
        {
            Id = tour.Id,
            AuthorId = tour.AuthorId,
            AuthorUsername = tour.AuthorUsername,
            Name = tour.Name,
            Description = tour.Description,
            Difficulty = tour.Difficulty,
            Tags = tour.Tags,
            Status = tour.Status,
            Price = tour.Price,
            CreatedAtUtc = tour.CreatedAtUtc
        };
    }
}
