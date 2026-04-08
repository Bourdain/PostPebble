using System.Security.Claims;

namespace Api.Infrastructure;

public static class CurrentUserProvider
{
    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
