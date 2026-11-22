using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ClaimsApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtClaim = System.Security.Claims.Claim;

namespace ClaimsApi.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public AccessToken CreateToken(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_jwt.ExpiresMinutes);

        var sub = new JwtClaim(JwtRegisteredClaimNames.Sub, user.Id.ToString());
        var role = new JwtClaim("role", user.Role);
        var userName = new JwtClaim(JwtRegisteredClaimNames.UniqueName, user.Username);

        JwtClaim[] claims = [sub, role, userName];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(tokenString, expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var refreshToken = RefreshTokenHasher.Generate();

        var now = timeProvider.GetUtcNow();

        var expiresAt = now.AddDays(_jwt.RefreshTokenDays);

        return new IssuedRefreshToken(refreshToken, expiresAt);
    }
}
