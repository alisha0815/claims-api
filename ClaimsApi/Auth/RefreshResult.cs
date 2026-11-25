using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Auth;

public record RefreshResult(RefreshStatus Status, LoginResponse? Response);
