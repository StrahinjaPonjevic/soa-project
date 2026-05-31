using System.Net;
using System.Text.Json;

namespace TourService.Services;

public class PurchaseAccessService : IPurchaseAccessService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PurchaseAccessService> _logger;

    public PurchaseAccessService(HttpClient httpClient, ILogger<PurchaseAccessService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> HasPurchasedTourAsync(int tourId, string authorizationHeader, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/purchases/tokens/{tourId}/exists");
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        using var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Purchase check failed for tourId={TourId}, status={Status}", tourId, (int)response.StatusCode);
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<PurchaseExistsResponse>(stream, _jsonOptions, ct);
        return payload?.Purchased ?? false;
    }

    public async Task<IReadOnlySet<int>> GetPurchasedTourIdsAsync(string authorizationHeader, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/purchases/tokens");
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        using var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new HashSet<int>();
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Purchase token list failed. Status={Status}", (int)response.StatusCode);
            return new HashSet<int>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<PurchaseTokensResponse>(stream, _jsonOptions, ct);
        var ids = payload?.Tokens?.Select(t => t.TourId).ToHashSet() ?? new HashSet<int>();
        return ids;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class PurchaseExistsResponse
    {
        public bool Purchased { get; set; }
    }

    private sealed class PurchaseTokensResponse
    {
        public List<PurchaseTokenItem>? Tokens { get; set; }
    }

    private sealed class PurchaseTokenItem
    {
        public int TourId { get; set; }
    }
}
