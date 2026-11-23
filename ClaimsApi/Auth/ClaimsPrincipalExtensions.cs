using System.Security.Claims;

namespace ClaimsApi.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        return int.Parse(userIdValue);
    }
}
