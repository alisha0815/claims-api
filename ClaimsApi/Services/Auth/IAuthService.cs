using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Services.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );

    Task<RegisterResult> CreateHandlerAsync(
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

    Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default);
}
