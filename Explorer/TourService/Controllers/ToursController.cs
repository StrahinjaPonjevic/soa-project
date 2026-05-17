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
    private readonly ITourReviewRepository _tourReviewRepository;

    public ToursController(
        AppDbContext context,
        ICurrentUserService currentUserService,
        ITourReviewRepository tourReviewRepository)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tourReviewRepository = tourReviewRepository;
    }

    [Authorize(Roles = "Guide")]
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

    [Authorize(Roles = "Guide")]
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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TourResponseDto>>> GetAll()
    {
        var tours = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
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

    [Authorize(Roles = "Guide")]
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

    [Authorize(Roles = "Guide")]
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

    [Authorize(Roles = "Guide")]
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

    [Authorize(Roles = "Tourist")]
    [HttpPost("{tourId:int}/reviews")]
    public async Task<ActionResult<TourReviewResponseDto>> AddReview(
        int tourId,
        [FromBody] CreateTourReviewDto dto,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var tourExists = await _context.Tours.AnyAsync(t => t.Id == tourId, cancellationToken);
        if (!tourExists)
        {
            return NotFound("Tour not found.");
        }

        var normalizedImageUrls = dto.ImageUrls?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        var invalidImageUrl = normalizedImageUrls
            .FirstOrDefault(url => !Uri.IsWellFormedUriString(url, UriKind.Absolute));
        if (invalidImageUrl is not null)
        {
            return BadRequest($"Invalid image URL: {invalidImageUrl}");
        }

        var review = new TourReview
        {
            TourId = tourId,
            TouristId = currentUser!.UserId,
            TouristUsername = currentUser.Username,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            VisitedAtUtc = dto.VisitedAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            ImageUrls = normalizedImageUrls
        };

        await _tourReviewRepository.AddAsync(review, cancellationToken);
        return CreatedAtAction(nameof(GetReviews), new { tourId }, ToResponse(review));
    }

    [HttpGet("{tourId:int}/reviews")]
    public async Task<ActionResult<IReadOnlyList<TourReviewResponseDto>>> GetReviews(
        int tourId,
        CancellationToken cancellationToken)
    {
        var tourExists = await _context.Tours.AnyAsync(t => t.Id == tourId, cancellationToken);
        if (!tourExists)
        {
            return NotFound("Tour not found.");
        }

        var reviews = await _tourReviewRepository.GetByTourIdAsync(tourId, cancellationToken);
        return Ok(reviews.Select(ToResponse).ToList());
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

    private static TourReviewResponseDto ToResponse(TourReview review)
    {
        return new TourReviewResponseDto
        {
            Id = review.Id,
            TourId = review.TourId,
            TouristId = review.TouristId,
            TouristUsername = review.TouristUsername,
            Rating = review.Rating,
            Comment = review.Comment,
            VisitedAtUtc = review.VisitedAtUtc,
            CreatedAtUtc = review.CreatedAtUtc,
            ImageUrls = review.ImageUrls
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
