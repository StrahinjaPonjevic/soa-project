using System.ComponentModel.DataAnnotations;

namespace TourService.DTOs;

public class UpdateKeyPointDto
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public required string Name { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public required string Description { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }
}
