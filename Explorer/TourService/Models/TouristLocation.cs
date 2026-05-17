namespace TourService.Models;

public class TouristLocation
{
    public int Id { get; set; }
    public int TouristId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
