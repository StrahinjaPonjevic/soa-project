using System.Security.Claims;

namespace BlogService.Services;

public class CurrentUserService : ICurrentUserService
{
    public bool TryGetCurrentUser(ClaimsPrincipal principal, out CurrentUser? currentUser)
    {
        currentUser = null;

        var userIdClaim =
            principal.FindFirst("userId")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        var username =
            principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("name")?.Value
            ?? principal.FindFirst("unique_name")?.Value;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        currentUser = new CurrentUser(userId, username);
        return true;
    }
}
