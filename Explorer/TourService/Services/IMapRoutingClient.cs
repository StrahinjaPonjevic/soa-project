using TourService.Models;

namespace TourService.Services;

public interface IMapRoutingClient
{
    Task<double> CalculateLengthKmAsync(IReadOnlyList<KeyPoint> orderedKeyPoints, CancellationToken cancellationToken);
    Task<IReadOnlyList<(double Latitude, double Longitude)>> GetRoutePreviewAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken);
}
