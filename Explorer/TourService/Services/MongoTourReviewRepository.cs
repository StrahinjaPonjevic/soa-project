using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TourService.Configuration;
using TourService.Models;

namespace TourService.Services;

public class MongoTourReviewRepository : ITourReviewRepository
{
    private readonly IMongoCollection<TourReview> _reviews;

    public MongoTourReviewRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _reviews = database.GetCollection<TourReview>(settings.ReviewsCollectionName);
    }

    public async Task AddAsync(TourReview review, CancellationToken cancellationToken = default)
    {
        await _reviews.InsertOneAsync(review, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<TourReview>> GetByTourIdAsync(int tourId, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviews.Find(r => r.TourId == tourId)
            .SortByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return reviews;
    }
}
