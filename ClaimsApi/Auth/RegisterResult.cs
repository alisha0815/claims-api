namespace ClaimsApi.Auth;

public record RegisterResult(int Id, string Username, string Role, DateTimeOffset CreatedAt);
