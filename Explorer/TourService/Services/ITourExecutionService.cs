using TourService.Models;

namespace TourService.Services;

public interface ITourExecutionService
{
    Task<TourExecution> StartTourAsync(int tourId, int touristId, string authorizationHeader, CancellationToken ct);
    Task<CheckNearbyResult> CheckNearbyKeyPointAsync(int executionId, int touristId, CancellationToken ct);
    Task<TourExecution> CompleteAsync(int executionId, int touristId, CancellationToken ct);
    Task<TourExecution> AbandonAsync(int executionId, int touristId, CancellationToken ct);
    Task CancelAsync(int executionId, CancellationToken ct);
    Task<TourExecution?> GetByIdAsync(int executionId, int touristId, CancellationToken ct);
    Task<TourExecution?> GetActiveForTouristAsync(int touristId, CancellationToken ct);
}

public sealed record CheckNearbyResult(
    bool IsNearKeyPoint,
    int? KeyPointId,
    string? KeyPointName,
    bool IsNewlyCompleted,
    bool AllKeyPointsCompleted,
    TourExecution Execution
);
