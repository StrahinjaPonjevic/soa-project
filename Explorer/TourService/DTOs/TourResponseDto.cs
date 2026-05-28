using TourService.Models;

namespace TourService.DTOs;

public class TourResponseDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public TourStatus Status { get; set; }
    public decimal Price { get; set; }
    public double LengthKm { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public IReadOnlyList<KeyPointResponseDto> KeyPoints { get; set; } = Array.Empty<KeyPointResponseDto>();
    public IReadOnlyList<TourTravelTimeDto> TravelTimes { get; set; } = Array.Empty<TourTravelTimeDto>();
}
