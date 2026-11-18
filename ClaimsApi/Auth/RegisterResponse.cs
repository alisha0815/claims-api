namespace ClaimsApi.Auth;

public record RegisterResponse(int Id, string Username, string Role, DateTimeOffset CreatedAt);
