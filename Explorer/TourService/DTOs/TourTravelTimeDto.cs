using System.ComponentModel.DataAnnotations;
using TourService.Models;

namespace TourService.DTOs;

public class TourTravelTimeDto
{
    public TransportType TransportType { get; set; }

    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; set; }
}
