using ClaimsApi.Dtos.Auth;

namespace ClaimsApi.Services.Auth;

public record RegisterResult(RegisterStatus Status, RegisterResponse? Response);
