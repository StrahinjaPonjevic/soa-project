namespace TourService.DTOs;

public class TourReviewResponseDto
{
    public string Id { get; set; } = string.Empty;
    public int TourId { get; set; }
    public int TouristId { get; set; }
    public string TouristUsername { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime VisitedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string[] ImageUrls { get; set; } = Array.Empty<string>();
}
