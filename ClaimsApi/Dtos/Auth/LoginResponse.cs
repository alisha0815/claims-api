namespace ClaimsApi.Dtos.Auth;

public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
