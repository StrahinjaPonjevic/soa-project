namespace TourService.DTOs;

public class RoutePreviewDto
{
    public IReadOnlyList<RoutePointDto> Points { get; set; } = Array.Empty<RoutePointDto>();
}

public class RoutePointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
