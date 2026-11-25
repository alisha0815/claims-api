using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Services.Auth;

public record RefreshResult(RefreshStatus Status, LoginResponse? Response);
