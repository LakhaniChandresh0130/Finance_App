using System.Security.Claims;

namespace Finance.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (id is null || !Guid.TryParse(id, out var guid))
            throw new UnauthorizedAccessException("User identifier is missing or invalid.");

        return guid;
    }
}
