namespace ClaimsApi.Auth.Tokens;

public record AccessToken(string Token, DateTimeOffset ExpiresAt);
