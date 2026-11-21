namespace ClaimsApi.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
