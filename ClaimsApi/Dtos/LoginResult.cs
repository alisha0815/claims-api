namespace ClaimsApi.Dtos;

public record LoginResult(LoginStatus Status, LoginResponse? Response);
