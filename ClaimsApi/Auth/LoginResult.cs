using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Auth;

public record LoginResult(LoginStatus Status, LoginResponse? Response);
