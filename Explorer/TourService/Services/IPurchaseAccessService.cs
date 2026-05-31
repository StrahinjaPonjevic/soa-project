namespace TourService.Services;

public interface IPurchaseAccessService
{
    Task<bool> HasPurchasedTourAsync(int tourId, string authorizationHeader, CancellationToken ct);
    Task<IReadOnlySet<int>> GetPurchasedTourIdsAsync(string authorizationHeader, CancellationToken ct);
}
