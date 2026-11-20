namespace ClaimsApi.Dtos;

public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
