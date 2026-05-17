using System.ComponentModel.DataAnnotations;

namespace TourService.DTOs;

public class CreateTourReviewDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 3)]
    public required string Comment { get; set; }

    public DateTime VisitedAtUtc { get; set; }

    public string[]? ImageUrls { get; set; }
}
