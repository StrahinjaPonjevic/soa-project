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
    public DateTime CreatedAtUtc { get; set; }
}
