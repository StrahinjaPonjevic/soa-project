using System.Security.Claims;

namespace TourService.Services;

public interface ICurrentUserService
{
    bool TryGetCurrentUser(ClaimsPrincipal principal, out CurrentUser? currentUser);
}
