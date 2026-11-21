namespace ClaimsApi.Auth;

public record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);
