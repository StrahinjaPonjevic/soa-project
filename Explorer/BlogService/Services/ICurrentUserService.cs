using System.Security.Claims;

namespace BlogService.Services;

public interface ICurrentUserService
{
    bool TryGetCurrentUser(ClaimsPrincipal principal, out CurrentUser? currentUser);
}
