using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Services.Auth;

public record LoginResult(LoginStatus Status, LoginResponse? Response);
