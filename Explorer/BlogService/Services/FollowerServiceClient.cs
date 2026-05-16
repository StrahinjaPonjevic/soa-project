using System.Text.Json;

namespace BlogService.Services;

public interface IFollowerServiceClient
{
    Task<bool> IsFollowingAsync(int followerId, int followingId);
}

public class FollowerServiceClient : IFollowerServiceClient
{
    private readonly HttpClient _http;

    public FollowerServiceClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("FollowerService");
    }

    public async Task<bool> IsFollowingAsync(int followerId, int followingId)
    {
        try
        {
            var response = await _http.GetAsync(
                $"/api/followers/is-following?followerId={followerId}&followingId={followingId}");

            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("isFollowing").GetBoolean();
        }
        catch
        {
            // Ako follower servis nije dostupan, ne blokiramo komentarisanje
            return false;
        }
    }
}
