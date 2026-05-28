using TourService.DTOs;
using TourService.Models;

namespace TourService.Services;

public interface ITourManagementService
{
    Task<Tour> CreateDraftTourAsync(CurrentUser currentUser, CreateTourDto dto, CancellationToken cancellationToken);
    Task<Tour> UpdateTourAsync(int tourId, CurrentUser currentUser, UpdateTourDto dto, CancellationToken cancellationToken);
    Task<KeyPoint> AddKeyPointAsync(int tourId, CurrentUser currentUser, CreateKeyPointDto dto, CancellationToken cancellationToken);
    Task<KeyPoint> UpdateKeyPointAsync(int tourId, int keyPointId, CurrentUser currentUser, UpdateKeyPointDto dto, CancellationToken cancellationToken);
    Task DeleteKeyPointAsync(int tourId, int keyPointId, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<IReadOnlyList<TourTravelTime>> ReplaceTravelTimesAsync(int tourId, CurrentUser currentUser, UpdateTourTravelTimesDto dto, CancellationToken cancellationToken);
    Task<Tour> PublishAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<Tour> ArchiveAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<Tour> ReactivateAsync(int tourId, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<IReadOnlyList<(double Latitude, double Longitude)>> GetRoutePreviewAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken);
}
