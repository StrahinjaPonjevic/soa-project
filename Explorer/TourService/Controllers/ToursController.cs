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
            .Include(t => t.KeyPoints)
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
            .Include(t => t.KeyPoints)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tour is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(tour));
    }

    [Authorize]
    [HttpPost("{tourId:int}/keypoints")]
    public async Task<ActionResult<KeyPointResponseDto>> AddKeyPoint(int tourId, [FromBody] CreateKeyPointDto dto)
    {
        var tour = await GetOwnedTourAsync(tourId);
        if (tour.Result is not null)
        {
            return tour.Result;
        }

        var keyPoint = new KeyPoint
        {
            TourId = tour.Value!.Id,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ImageUrl = NormalizeOptionalText(dto.ImageUrl),
            OrderIndex = dto.OrderIndex
        };

        await _context.KeyPoints.AddAsync(keyPoint);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tourId }, ToResponse(keyPoint));
    }

    [HttpGet("{tourId:int}/keypoints")]
    public async Task<ActionResult<IReadOnlyList<KeyPointResponseDto>>> GetKeyPoints(int tourId)
    {
        var tourExists = await _context.Tours.AnyAsync(t => t.Id == tourId);
        if (!tourExists)
        {
            return NotFound("Tour not found.");
        }

        var keyPoints = await _context.KeyPoints
            .AsNoTracking()
            .Where(k => k.TourId == tourId)
            .OrderBy(k => k.OrderIndex)
            .ThenBy(k => k.Id)
            .Select(k => ToResponse(k))
            .ToListAsync();

        return Ok(keyPoints);
    }

    [Authorize]
    [HttpPut("{tourId:int}/keypoints/{keyPointId:int}")]
    public async Task<ActionResult<KeyPointResponseDto>> UpdateKeyPoint(
        int tourId,
        int keyPointId,
        [FromBody] UpdateKeyPointDto dto)
    {
        var tour = await GetOwnedTourAsync(tourId);
        if (tour.Result is not null)
        {
            return tour.Result;
        }

        var keyPoint = await _context.KeyPoints
            .FirstOrDefaultAsync(k => k.TourId == tour.Value!.Id && k.Id == keyPointId);

        if (keyPoint is null)
        {
            return NotFound("Key point not found.");
        }

        keyPoint.Name = dto.Name.Trim();
        keyPoint.Description = dto.Description.Trim();
        keyPoint.Latitude = dto.Latitude;
        keyPoint.Longitude = dto.Longitude;
        keyPoint.ImageUrl = NormalizeOptionalText(dto.ImageUrl);
        keyPoint.OrderIndex = dto.OrderIndex;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(keyPoint));
    }

    [Authorize]
    [HttpDelete("{tourId:int}/keypoints/{keyPointId:int}")]
    public async Task<IActionResult> DeleteKeyPoint(int tourId, int keyPointId)
    {
        var tour = await GetOwnedTourAsync(tourId);
        if (tour.Result is not null)
        {
            return tour.Result;
        }

        var keyPoint = await _context.KeyPoints
            .FirstOrDefaultAsync(k => k.TourId == tour.Value!.Id && k.Id == keyPointId);

        if (keyPoint is null)
        {
            return NotFound("Key point not found.");
        }

        _context.KeyPoints.Remove(keyPoint);
        await _context.SaveChangesAsync();

        return NoContent();
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
            CreatedAtUtc = tour.CreatedAtUtc,
            KeyPoints = tour.KeyPoints
                .OrderBy(k => k.OrderIndex)
                .ThenBy(k => k.Id)
                .Select(ToResponse)
                .ToList()
        };
    }

    private static KeyPointResponseDto ToResponse(KeyPoint keyPoint)
    {
        return new KeyPointResponseDto
        {
            Id = keyPoint.Id,
            TourId = keyPoint.TourId,
            Name = keyPoint.Name,
            Description = keyPoint.Description,
            Latitude = keyPoint.Latitude,
            Longitude = keyPoint.Longitude,
            ImageUrl = keyPoint.ImageUrl,
            OrderIndex = keyPoint.OrderIndex
        };
    }

    private async Task<ActionResult<Tour>> GetOwnedTourAsync(int tourId)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId);
        if (tour is null)
        {
            return NotFound("Tour not found.");
        }

        if (tour.AuthorId != currentUser!.UserId)
        {
            return Forbid();
        }

        return tour;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
