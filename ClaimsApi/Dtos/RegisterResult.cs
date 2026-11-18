namespace ClaimsApi.Dtos;

public record RegisterResult(int Id, string Username, string Role, DateTimeOffset CreatedAt);
