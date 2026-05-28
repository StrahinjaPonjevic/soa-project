namespace TourService.Configuration;

public class MapRoutingSettings
{
    public const string SectionName = "MapRouting";

    public string BaseUrl { get; set; } = "https://router.project-osrm.org";
    public string Profile { get; set; } = "foot";
}
