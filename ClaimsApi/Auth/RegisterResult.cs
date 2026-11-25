using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Auth;

public record RegisterResult(RegisterStatus Status, RegisterResponse? Response);
