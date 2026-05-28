using System.ComponentModel.DataAnnotations;

namespace TourService.DTOs;

public class UpdateTourDto
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public required string Name { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public required string Description { get; set; }

    [Required]
    [StringLength(32, MinimumLength = 3)]
    public required string Difficulty { get; set; }

    public string[]? Tags { get; set; }
}
