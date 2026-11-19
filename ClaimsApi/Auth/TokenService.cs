using ClaimsApi.Models;
using Microsoft.Extensions.Options;

namespace ClaimsApi.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public AccessToken CreateToken(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.Value.ExpiresMinutes);

        Console.WriteLine($"now = {now}, expiresAt = {expiresAt}");

        // return access token (Token, ExpiresAt)
        return new AccessToken("TODO", expiresAt);
    }
}
