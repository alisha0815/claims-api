using ClaimsApi.Dtos;
using ClaimsApi.Models;

namespace ClaimsApi.Auth;

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
