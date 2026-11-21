using ClaimsApi.Dtos;

namespace ClaimsApi.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );

    Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );

    Task<RefreshResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default
    );
}
