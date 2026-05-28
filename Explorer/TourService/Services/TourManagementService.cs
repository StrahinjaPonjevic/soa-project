using Microsoft.EntityFrameworkCore;
using TourService.Data;
using TourService.DTOs;
using TourService.Models;

namespace TourService.Services;

public class TourManagementService : ITourManagementService
{
    private readonly AppDbContext _context;
    private readonly IMapRoutingClient _mapRoutingClient;

    public TourManagementService(AppDbContext context, IMapRoutingClient mapRoutingClient)
    {
        _context = context;
        _mapRoutingClient = mapRoutingClient;
    }

    public async Task<Tour> CreateDraftTourAsync(
        CurrentUser currentUser,
        CreateTourDto dto,
        CancellationToken cancellationToken)
    {
        var tour = new Tour
        {
            AuthorId = currentUser.UserId,
            AuthorUsername = currentUser.Username,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Difficulty = dto.Difficulty.Trim(),
            Tags = NormalizeTags(dto.Tags),
            Status = TourStatus.Draft,
            Price = 0,
            LengthKm = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Tours.AddAsync(tour, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task<Tour> UpdateTourAsync(
        int tourId,
        CurrentUser currentUser,
        UpdateTourDto dto,
        CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);
        tour.Name = dto.Name.Trim();
        tour.Description = dto.Description.Trim();
        tour.Difficulty = dto.Difficulty.Trim();
        tour.Tags = NormalizeTags(dto.Tags);

        EnsurePublishedTourStillValid(tour);

        await _context.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task<KeyPoint> AddKeyPointAsync(
        int tourId,
        CurrentUser currentUser,
        CreateKeyPointDto dto,
        CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);
        EnsureOrderIndexAvailable(tour, dto.OrderIndex);

        var keyPoint = new KeyPoint
        {
            TourId = tour.Id,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ImageUrl = NormalizeOptionalText(dto.ImageUrl),
            OrderIndex = dto.OrderIndex
        };

        tour.KeyPoints.Add(keyPoint);
        await RecalculateLengthAsync(tour, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return keyPoint;
    }

    public async Task<KeyPoint> UpdateKeyPointAsync(
        int tourId,
        int keyPointId,
        CurrentUser currentUser,
        UpdateKeyPointDto dto,
        CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);
        var keyPoint = tour.KeyPoints.FirstOrDefault(k => k.Id == keyPointId)
            ?? throw new TourOperationException(404, "Key point not found.");

        EnsureOrderIndexAvailable(tour, dto.OrderIndex, keyPointId);

        keyPoint.Name = dto.Name.Trim();
        keyPoint.Description = dto.Description.Trim();
        keyPoint.Latitude = dto.Latitude;
        keyPoint.Longitude = dto.Longitude;
        keyPoint.ImageUrl = NormalizeOptionalText(dto.ImageUrl);
        keyPoint.OrderIndex = dto.OrderIndex;

        await RecalculateLengthAsync(tour, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return keyPoint;
    }

    public async Task DeleteKeyPointAsync(
        int tourId,
        int keyPointId,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);
        var keyPoint = tour.KeyPoints.FirstOrDefault(k => k.Id == keyPointId)
            ?? throw new TourOperationException(404, "Key point not found.");

        if (tour.Status == TourStatus.Published && tour.KeyPoints.Count <= 2)
        {
            throw new TourOperationException(400, "Published tour must keep at least two key points.");
        }

        tour.KeyPoints.Remove(keyPoint);
        _context.KeyPoints.Remove(keyPoint);

        await RecalculateLengthAsync(tour, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TourTravelTime>> ReplaceTravelTimesAsync(
        int tourId,
        CurrentUser currentUser,
        UpdateTourTravelTimesDto dto,
        CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);
        var normalizedTravelTimes = NormalizeTravelTimes(dto.TravelTimes);

        if (tour.Status == TourStatus.Published &&
            normalizedTravelTimes.Count == 0)
        {
            throw new TourOperationException(400, "Published tour must keep at least one travel time.");
        }

        _context.TourTravelTimes.RemoveRange(tour.TravelTimes);
        tour.TravelTimes.Clear();

        foreach (var travelTime in normalizedTravelTimes)
        {
            tour.TravelTimes.Add(travelTime);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return tour.TravelTimes.OrderBy(tt => tt.TransportType).ToList();
    }

    public async Task<Tour> PublishAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);

        if (tour.Status == TourStatus.Published)
        {
            throw new TourOperationException(400, "Tour is already published.");
        }

        if (tour.Status == TourStatus.Archived)
        {
            throw new TourOperationException(400, "Archived tours must be reactivated instead of published again.");
        }

        var validationErrors = ValidatePublishRules(tour);
        if (validationErrors.Count > 0)
        {
            throw new TourOperationException(400, "Tour cannot be published.", validationErrors);
        }

        tour.Status = TourStatus.Published;
        tour.PublishedAtUtc = DateTime.UtcNow;
        tour.ArchivedAtUtc = null;

        await _context.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task<Tour> ArchiveAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);

        if (tour.Status != TourStatus.Published)
        {
            throw new TourOperationException(400, "Only published tours can be archived.");
        }

        tour.Status = TourStatus.Archived;
        tour.ArchivedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task<Tour> ReactivateAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var tour = await GetOwnedTourWithRelationsAsync(tourId, currentUser.UserId, cancellationToken);

        if (tour.Status != TourStatus.Archived)
        {
            throw new TourOperationException(400, "Only archived tours can be reactivated.");
        }

        var validationErrors = ValidatePublishRules(tour);
        if (validationErrors.Count > 0)
        {
            throw new TourOperationException(400, "Tour cannot be reactivated.", validationErrors);
        }

        tour.Status = TourStatus.Published;
        tour.ArchivedAtUtc = null;

        await _context.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public Task<IReadOnlyList<(double Latitude, double Longitude)>> GetRoutePreviewAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken)
    {
        return _mapRoutingClient.GetRoutePreviewAsync(orderedKeyPoints, cancellationToken);
    }

    private async Task<Tour> GetOwnedTourWithRelationsAsync(int tourId, int authorId, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours
            .Include(t => t.KeyPoints)
            .Include(t => t.TravelTimes)
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);

        if (tour is null)
        {
            throw new TourOperationException(404, "Tour not found.");
        }

        if (tour.AuthorId != authorId)
        {
            throw new TourOperationException(403, "You are not allowed to modify this tour.");
        }

        return tour;
    }

    private static string[] NormalizeTags(IEnumerable<string>? tags)
    {
        return tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void EnsureOrderIndexAvailable(Tour tour, int orderIndex, int? keyPointId = null)
    {
        var orderIndexExists = tour.KeyPoints.Any(k => k.Id != keyPointId && k.OrderIndex == orderIndex);
        if (orderIndexExists)
        {
            throw new TourOperationException(400, "A key point with the same order index already exists for this tour.");
        }
    }

    private async Task RecalculateLengthAsync(Tour tour, CancellationToken cancellationToken)
    {
        var orderedKeyPoints = tour.KeyPoints
            .OrderBy(k => k.OrderIndex)
            .ThenBy(k => k.Id)
            .ToList();

        if (orderedKeyPoints.Count < 2)
        {
            tour.LengthKm = 0;
            return;
        }

        tour.LengthKm = await _mapRoutingClient.CalculateLengthKmAsync(orderedKeyPoints, cancellationToken);
    }

    private void EnsurePublishedTourStillValid(Tour tour)
    {
        if (tour.Status != TourStatus.Published)
        {
            return;
        }

        var validationErrors = ValidatePublishRules(tour);
        if (validationErrors.Count > 0)
        {
            throw new TourOperationException(400, "Published tour cannot be changed in a way that breaks publish rules.", validationErrors);
        }
    }

    private static List<string> ValidatePublishRules(Tour tour)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(tour.Name))
        {
            errors.Add("Tour name is required.");
        }

        if (string.IsNullOrWhiteSpace(tour.Description))
        {
            errors.Add("Tour description is required.");
        }

        if (string.IsNullOrWhiteSpace(tour.Difficulty))
        {
            errors.Add("Tour difficulty is required.");
        }

        if (tour.Tags.Length == 0)
        {
            errors.Add("At least one tag is required.");
        }

        if (tour.KeyPoints.Count < 2)
        {
            errors.Add("At least two key points are required.");
        }

        if (tour.TravelTimes.Count == 0)
        {
            errors.Add("At least one travel time is required.");
        }

        return errors;
    }

    private static List<TourTravelTime> NormalizeTravelTimes(IReadOnlyList<TourTravelTimeDto> travelTimes)
    {
        var duplicateType = travelTimes
            .GroupBy(tt => tt.TransportType)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateType is not null)
        {
            throw new TourOperationException(400, $"Duplicate travel time for transport type {duplicateType.Key}.");
        }

        return travelTimes
            .Select(tt => new TourTravelTime
            {
                TransportType = tt.TransportType,
                DurationMinutes = tt.DurationMinutes
            })
            .ToList();
    }

    private static string ToTransportKey(TourTravelTime travelTime)
    {
        return travelTime.TransportType.ToString();
    }
}
