using TourService.Models;

namespace TourService.Services;

public interface ITourReviewRepository
{
    Task AddAsync(TourReview review, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourReview>> GetByTourIdAsync(int tourId, CancellationToken cancellationToken = default);
}
