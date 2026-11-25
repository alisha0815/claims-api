using ClaimsApi.Models;

namespace ClaimsApi.Auth.Tokens;

public interface ITokenService
{
    AccessToken CreateToken(User user);
    IssuedRefreshToken CreateRefreshToken();
}
