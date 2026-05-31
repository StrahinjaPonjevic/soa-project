using Microsoft.EntityFrameworkCore;
using TourService.Data;
using TourService.Models;

namespace TourService.Services;

public class TourExecutionService : ITourExecutionService
{
    private const double ProximityThresholdKm = 0.2;

    private readonly AppDbContext _context;

    public TourExecutionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TourExecution> StartTourAsync(int tourId, int touristId, CancellationToken ct)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tourId, ct);

        if (tour is null)
            throw new TourOperationException(404, "Tour not found.");

        if (tour.Status != TourStatus.Published && tour.Status != TourStatus.Archived)
            throw new TourOperationException(400, "Only published or archived tours can be started.");

        var existing = await _context.TourExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TouristId == touristId && e.Status == TourExecutionStatus.Active, ct);

        if (existing is not null)
            throw new TourOperationException(409, "Tourist already has an active tour execution.");

        var location = await _context.TouristLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TouristId == touristId, ct);

        if (location is null)
            throw new TourOperationException(400, "Tourist location is not set. Use the position simulator first.");

        var now = DateTime.UtcNow;
        var execution = new TourExecution
        {
            TourId = tourId,
            TouristId = touristId,
            Status = TourExecutionStatus.Active,
            StartedAtUtc = now,
            LastActivityAtUtc = now,
            InitialLatitude = location.Latitude,
            InitialLongitude = location.Longitude
        };

        _context.TourExecutions.Add(execution);
        await _context.SaveChangesAsync(ct);
        return execution;
    }

    public async Task<CheckNearbyResult> CheckNearbyKeyPointAsync(int executionId, int touristId, CancellationToken ct)
    {
        var execution = await _context.TourExecutions
            .Include(e => e.CompletedKeyPoints)
            .FirstOrDefaultAsync(e => e.Id == executionId && e.TouristId == touristId && e.Status == TourExecutionStatus.Active, ct);

        if (execution is null)
            throw new TourOperationException(404, "Active tour execution not found.");

        execution.LastActivityAtUtc = DateTime.UtcNow;

        var location = await _context.TouristLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TouristId == touristId, ct);

        if (location is null)
        {
            await _context.SaveChangesAsync(ct);
            return new CheckNearbyResult(false, null, null, false, execution);
        }

        var tour = await _context.Tours
            .Include(t => t.KeyPoints)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == execution.TourId, ct);

        var completedIds = execution.CompletedKeyPoints.Select(ckp => ckp.KeyPointId).ToHashSet();

        KeyPoint? nearest = null;
        double minDist = double.MaxValue;

        foreach (var kp in tour!.KeyPoints.Where(kp => !completedIds.Contains(kp.Id)))
        {
            var dist = HaversineKm(location.Latitude, location.Longitude, kp.Latitude, kp.Longitude);
            if (dist < ProximityThresholdKm && dist < minDist)
            {
                minDist = dist;
                nearest = kp;
            }
        }

        if (nearest is null)
        {
            await _context.SaveChangesAsync(ct);
            return new CheckNearbyResult(false, null, null, false, execution);
        }

        var completed = new CompletedKeyPoint
        {
            TourExecutionId = executionId,
            KeyPointId = nearest.Id,
            CompletedAtUtc = DateTime.UtcNow
        };
        _context.CompletedKeyPoints.Add(completed);
        await _context.SaveChangesAsync(ct);

        return new CheckNearbyResult(true, nearest.Id, nearest.Name, true, execution);
    }

    public async Task<TourExecution> CompleteAsync(int executionId, int touristId, CancellationToken ct)
    {
        var execution = await GetActiveOrThrow(executionId, touristId, ct);
        var now = DateTime.UtcNow;
        execution.Status = TourExecutionStatus.Completed;
        execution.CompletedAtUtc = now;
        execution.LastActivityAtUtc = now;
        await _context.SaveChangesAsync(ct);
        return execution;
    }

    public async Task<TourExecution> AbandonAsync(int executionId, int touristId, CancellationToken ct)
    {
        var execution = await GetActiveOrThrow(executionId, touristId, ct);
        var now = DateTime.UtcNow;
        execution.Status = TourExecutionStatus.Abandoned;
        execution.AbandonedAtUtc = now;
        execution.LastActivityAtUtc = now;
        await _context.SaveChangesAsync(ct);
        return execution;
    }

    public async Task CancelAsync(int executionId, CancellationToken ct)
    {
        var execution = await _context.TourExecutions
            .FirstOrDefaultAsync(e => e.Id == executionId, ct);

        if (execution is null) return;

        execution.Status = TourExecutionStatus.Cancelled;
        execution.LastActivityAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<TourExecution?> GetByIdAsync(int executionId, int touristId, CancellationToken ct)
    {
        return await _context.TourExecutions
            .Include(e => e.CompletedKeyPoints)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId && e.TouristId == touristId, ct);
    }

    public async Task<TourExecution?> GetActiveForTouristAsync(int touristId, CancellationToken ct)
    {
        return await _context.TourExecutions
            .Include(e => e.CompletedKeyPoints)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TouristId == touristId && e.Status == TourExecutionStatus.Active, ct);
    }

    private async Task<TourExecution> GetActiveOrThrow(int executionId, int touristId, CancellationToken ct)
    {
        var execution = await _context.TourExecutions
            .FirstOrDefaultAsync(e => e.Id == executionId && e.TouristId == touristId && e.Status == TourExecutionStatus.Active, ct);

        if (execution is null)
            throw new TourOperationException(404, "Active tour execution not found.");

        return execution;
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
