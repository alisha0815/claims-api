using ClaimsApi.Dtos;

namespace ClaimsApi.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
}
