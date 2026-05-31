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
    private readonly ITourManagementService _tourManagementService;
    private readonly ITourReviewRepository _tourReviewRepository;
    private readonly IPurchaseAccessService _purchaseAccessService;

    public ToursController(
        AppDbContext context,
        ICurrentUserService currentUserService,
        ITourManagementService tourManagementService,
        ITourReviewRepository tourReviewRepository,
        IPurchaseAccessService purchaseAccessService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tourManagementService = tourManagementService;
        _tourReviewRepository = tourReviewRepository;
        _purchaseAccessService = purchaseAccessService;
    }

    [Authorize(Roles = "Guide")]
    [HttpPost]
    public async Task<ActionResult<TourResponseDto>> Create([FromBody] CreateTourDto dto, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var tour = await _tourManagementService.CreateDraftTourAsync(currentUser, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = tour.Id }, ToAuthorResponse(tour));
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<TourResponseDto>>> GetMine(CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        var tours = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
            .Include(t => t.TravelTimes)
            .Where(t => t.AuthorId == currentUser.UserId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(tours.Select(ToAuthorResponse).ToList());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TourResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tours = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
            .Include(t => t.TravelTimes)
            .Where(t => t.Status == TourStatus.Published || t.Status == TourStatus.Archived)
            .OrderByDescending(t => t.PublishedAtUtc ?? t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var currentUser = GetCurrentUserOrNull();
        if (currentUser is null || !IsTourist())
        {
            return Ok(tours.Select(ToPublicResponse).ToList());
        }

        var authHeader = Request.Headers.Authorization.ToString();
        var purchasedIds = await _purchaseAccessService.GetPurchasedTourIdsAsync(authHeader, cancellationToken);
        return Ok(tours.Select(t => ToResponse(t, purchasedIds.Contains(t.Id))).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TourResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
            .Include(t => t.TravelTimes)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tour is null)
        {
            return NotFound();
        }

        var currentUser = GetCurrentUserOrNull();
        var isOwner = currentUser is not null && currentUser.UserId == tour.AuthorId;
        if (isOwner)
        {
            return Ok(ToAuthorResponse(tour));
        }

        if (tour.Status != TourStatus.Published && tour.Status != TourStatus.Archived)
        {
            return NotFound();
        }

        var includeAllKeyPoints = false;
        if (currentUser is not null && IsTourist())
        {
            var authHeader = Request.Headers.Authorization.ToString();
            includeAllKeyPoints = await _purchaseAccessService.HasPurchasedTourAsync(id, authHeader, cancellationToken);
        }

        return Ok(ToResponse(tour, includeAllKeyPoints));
    }

    [Authorize(Roles = "Guide")]
    [HttpPut("{tourId:int}")]
    public async Task<ActionResult<TourResponseDto>> Update(
        int tourId,
        [FromBody] UpdateTourDto dto,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var tour = await _tourManagementService.UpdateTourAsync(tourId, currentUser, dto, cancellationToken);
            return Ok(ToAuthorResponse(tour));
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpPost("{tourId:int}/keypoints")]
    public async Task<ActionResult<KeyPointResponseDto>> AddKeyPoint(
        int tourId,
        [FromBody] CreateKeyPointDto dto,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var keyPoint = await _tourManagementService.AddKeyPointAsync(tourId, currentUser, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = tourId }, ToResponse(keyPoint));
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [HttpGet("{tourId:int}/keypoints")]
    public async Task<ActionResult<IReadOnlyList<KeyPointResponseDto>>> GetKeyPoints(int tourId, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);

        if (tour is null)
        {
            return NotFound("Tour not found.");
        }

        var currentUser = GetCurrentUserOrNull();
        var isOwner = currentUser is not null && currentUser.UserId == tour.AuthorId;
        if (!isOwner && tour.Status != TourStatus.Published && tour.Status != TourStatus.Archived)
        {
            return NotFound("Tour not found.");
        }

        var keyPoints = tour.KeyPoints
            .OrderBy(k => k.OrderIndex)
            .ThenBy(k => k.Id)
            .Select(ToResponse)
            .ToList();

        if (!isOwner && keyPoints.Count > 1)
        {
            var includeAll = false;
            if (currentUser is not null && IsTourist())
            {
                var authHeader = Request.Headers.Authorization.ToString();
                includeAll = await _purchaseAccessService.HasPurchasedTourAsync(tourId, authHeader, cancellationToken);
            }

            if (!includeAll)
            {
                keyPoints = keyPoints.Take(1).ToList();
            }
        }

        return Ok(keyPoints);
    }

    [HttpGet("{tourId:int}/route-preview")]
    public async Task<ActionResult<RoutePreviewDto>> GetRoutePreview(int tourId, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .Include(t => t.KeyPoints)
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);

        if (tour is null)
        {
            return NotFound("Tour not found.");
        }

        var currentUser = GetCurrentUserOrNull();
        var isOwner = currentUser is not null && currentUser.UserId == tour.AuthorId;
        var isPurchasedTourist = false;
        if (!isOwner && currentUser is not null && IsTourist())
        {
            var authHeader = Request.Headers.Authorization.ToString();
            isPurchasedTourist = await _purchaseAccessService.HasPurchasedTourAsync(tourId, authHeader, cancellationToken);
        }

        if (!isOwner && !isPurchasedTourist && tour.Status != TourStatus.Published)
        {
            return NotFound("Tour not found.");
        }

        if (!isOwner && !isPurchasedTourist)
        {
            // Tourists must not see the rest of the route geometry.
            return Ok(new RoutePreviewDto());
        }

        var orderedKeyPoints = tour.KeyPoints
            .OrderBy(k => k.OrderIndex)
            .ThenBy(k => k.Id)
            .ToList();

        if (orderedKeyPoints.Count < 2)
        {
            return Ok(new RoutePreviewDto());
        }

        try
        {
            var routePoints = await _tourManagementService.GetRoutePreviewAsync(orderedKeyPoints, cancellationToken);
            return Ok(new RoutePreviewDto
            {
                Points = routePoints
                    .Select(point => new RoutePointDto
                    {
                        Latitude = point.Latitude,
                        Longitude = point.Longitude
                    })
                    .ToList()
            });
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpPut("{tourId:int}/keypoints/{keyPointId:int}")]
    public async Task<ActionResult<KeyPointResponseDto>> UpdateKeyPoint(
        int tourId,
        int keyPointId,
        [FromBody] UpdateKeyPointDto dto,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var keyPoint = await _tourManagementService.UpdateKeyPointAsync(tourId, keyPointId, currentUser, dto, cancellationToken);
            return Ok(ToResponse(keyPoint));
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpDelete("{tourId:int}/keypoints/{keyPointId:int}")]
    public async Task<IActionResult> DeleteKeyPoint(int tourId, int keyPointId, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            await _tourManagementService.DeleteKeyPointAsync(tourId, keyPointId, currentUser, cancellationToken);
            return NoContent();
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpPut("{tourId:int}/travel-times")]
    public async Task<ActionResult<IReadOnlyList<TourTravelTimeDto>>> ReplaceTravelTimes(
        int tourId,
        [FromBody] UpdateTourTravelTimesDto dto,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var travelTimes = await _tourManagementService.ReplaceTravelTimesAsync(tourId, currentUser, dto, cancellationToken);
            return Ok(travelTimes
                .OrderBy(tt => tt.TransportType)
                .Select(ToResponse)
                .ToList());
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Guide")]
    [HttpPost("{tourId:int}/reactivate")]
    public async Task<ActionResult<TourResponseDto>> Reactivate(int tourId, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        try
        {
            var tour = await _tourManagementService.ReactivateAsync(tourId, currentUser, cancellationToken);
            return Ok(ToAuthorResponse(tour));
        }
        catch (TourOperationException ex)
        {
            return ToErrorResult(ex);
        }
    }

    [Authorize(Roles = "Tourist")]
    [HttpPost("{tourId:int}/reviews")]
    public async Task<ActionResult<TourReviewResponseDto>> AddReview(
        int tourId,
        [FromBody] CreateTourReviewDto dto,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUserOrUnauthorized();
        if (currentUser is null)
        {
            return Unauthorized("Missing user claims in token.");
        }

        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);

        if (tour is null || tour.Status != TourStatus.Published)
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
            TouristId = currentUser.UserId,
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
        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);

        if (tour is null)
        {
            return NotFound("Tour not found.");
        }

        var currentUser = GetCurrentUserOrNull();
        var isOwner = currentUser is not null && currentUser.UserId == tour.AuthorId;
        if (!isOwner && tour.Status != TourStatus.Published)
        {
            return NotFound("Tour not found.");
        }

        var reviews = await _tourReviewRepository.GetByTourIdAsync(tourId, cancellationToken);
        return Ok(reviews.Select(ToResponse).ToList());
    }

    public static TourResponseDto ToAuthorResponse(Tour tour)
    {
        return ToResponse(tour, includeAllKeyPoints: true);
    }

    public static TourResponseDto ToPublicResponse(Tour tour)
    {
        return ToResponse(tour, includeAllKeyPoints: false);
    }

    private static TourResponseDto ToResponse(Tour tour, bool includeAllKeyPoints)
    {
        IEnumerable<KeyPoint> orderedKeyPoints = tour.KeyPoints
            .OrderBy(k => k.OrderIndex)
            .ThenBy(k => k.Id);

        if (!includeAllKeyPoints)
        {
            orderedKeyPoints = orderedKeyPoints.Take(1);
        }

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
            LengthKm = tour.LengthKm,
            CreatedAtUtc = tour.CreatedAtUtc,
            PublishedAtUtc = tour.PublishedAtUtc,
            ArchivedAtUtc = tour.ArchivedAtUtc,
            KeyPoints = orderedKeyPoints.Select(ToResponse).ToList(),
            TravelTimes = tour.TravelTimes
                .OrderBy(tt => tt.TransportType)
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

    private static TourTravelTimeDto ToResponse(TourTravelTime travelTime)
    {
        return new TourTravelTimeDto
        {
            TransportType = travelTime.TransportType,
            DurationMinutes = travelTime.DurationMinutes
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

    private CurrentUser? GetCurrentUserOrUnauthorized()
    {
        return _currentUserService.TryGetCurrentUser(User, out var currentUser) ? currentUser : null;
    }

    private CurrentUser? GetCurrentUserOrNull()
    {
        _currentUserService.TryGetCurrentUser(User, out var currentUser);
        return currentUser;
    }

    private bool IsTourist()
    {
        return User.IsInRole("Tourist") ||
               User.Claims.Any(c =>
                   (c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                   && c.Value == "Tourist");
    }

    private ActionResult ToErrorResult(TourOperationException ex)
    {
        return StatusCode(ex.StatusCode, new
        {
            message = ex.Message,
            errors = ex.Errors
        });
    }
}
