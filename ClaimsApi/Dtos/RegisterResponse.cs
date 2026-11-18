namespace ClaimsApi.Dtos;

public record RegisterResponse(int Id, string Username, string Role, DateTimeOffset CreatedAt);
