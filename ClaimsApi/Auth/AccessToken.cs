namespace ClaimsApi.Auth;

public record AccessToken(string Token, DateTimeOffset ExpiresAt);
