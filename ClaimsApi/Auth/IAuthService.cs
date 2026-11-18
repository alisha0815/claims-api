using ClaimsApi.Dtos;

namespace ClaimsApi.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
}
