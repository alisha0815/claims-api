namespace ClaimsApi.Auth.Tokens;

public record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);
