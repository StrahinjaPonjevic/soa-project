using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TourService.Configuration;
using TourService.Models;

namespace TourService.Services;

public class OsrmMapRoutingClient : IMapRoutingClient
{
    private readonly HttpClient _httpClient;
    private readonly MapRoutingSettings _settings;

    public OsrmMapRoutingClient(HttpClient httpClient, IOptions<MapRoutingSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<double> CalculateLengthKmAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken)
    {
        var route = await GetRouteDocumentAsync(orderedKeyPoints, cancellationToken);
        var distanceMeters = route.GetProperty("distance").GetDouble();
        return Math.Round(distanceMeters / 1000d, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<IReadOnlyList<(double Latitude, double Longitude)>> GetRoutePreviewAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken)
    {
        if (orderedKeyPoints.Count < 2)
        {
            return Array.Empty<(double Latitude, double Longitude)>();
        }

        var route = await GetRouteDocumentAsync(orderedKeyPoints, cancellationToken);
        var coordinates = route.GetProperty("geometry").GetProperty("coordinates");

        return coordinates
            .EnumerateArray()
            .Select(point => (Latitude: point[1].GetDouble(), Longitude: point[0].GetDouble()))
            .ToList();
    }

    private async Task<JsonElement> GetRouteDocumentAsync(
        IReadOnlyList<KeyPoint> orderedKeyPoints,
        CancellationToken cancellationToken)
    {
        if (orderedKeyPoints.Count < 2)
        {
            throw new TourOperationException(400, "At least two key points are required to build a route.");
        }

        var coordinates = string.Join(
            ";",
            orderedKeyPoints.Select(k =>
                $"{k.Longitude.ToString(CultureInfo.InvariantCulture)},{k.Latitude.ToString(CultureInfo.InvariantCulture)}"));

        var routeUrl = $"/route/v1/{_settings.Profile}/{coordinates}?overview=full&geometries=geojson";
        using var response = await _httpClient.GetAsync(routeUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new TourOperationException(502, "Map routing service is unavailable.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var routes = json.RootElement.GetProperty("routes");
        if (routes.GetArrayLength() == 0)
        {
            throw new TourOperationException(502, "Map routing service did not return a route.");
        }

        return routes[0].Clone();
    }
}
