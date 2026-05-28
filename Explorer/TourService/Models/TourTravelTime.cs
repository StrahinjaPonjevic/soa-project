namespace TourService.Models;

public class TourTravelTime
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public Tour? Tour { get; set; }
    public TransportType TransportType { get; set; }
    public int DurationMinutes { get; set; }
}
