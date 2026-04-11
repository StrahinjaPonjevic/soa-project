using AuthService.Services;

public class StakeholdersClient : IStakeholdersClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StakeholdersClient> _logger;

    public StakeholdersClient(HttpClient httpClient, ILogger<StakeholdersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitializeProfileAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Šaljem init ka: {BaseAddress}api/profiles/init za userId={UserId}",
                _httpClient.BaseAddress, userId);

            var response = await _httpClient.PostAsJsonAsync(
                "api/profiles/init",
                new { UserId = userId });

            _logger.LogInformation("Odgovor: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Body odgovora: {Body}", body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri komunikaciji sa Stakeholders servisom za userId={UserId}", userId);
        }
    }
}