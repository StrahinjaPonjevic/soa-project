namespace TourService.Configuration;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "tour_reviews_db";
    public string ReviewsCollectionName { get; set; } = "tour_reviews";
}
