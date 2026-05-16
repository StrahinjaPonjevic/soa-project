namespace TourService.DTOs;

public class KeyPointResponseDto
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
}
