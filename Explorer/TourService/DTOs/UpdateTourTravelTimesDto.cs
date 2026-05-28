using System.ComponentModel.DataAnnotations;

namespace TourService.DTOs;

public class UpdateTourTravelTimesDto
{
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<TourTravelTimeDto> TravelTimes { get; set; }
}
