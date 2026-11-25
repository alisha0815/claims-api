using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Auth.Tokens;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    // Access tokens stay short-lived, refresh tokens handle longer sessions.
    [Range(1, 120, ErrorMessage = "Jwt:ExpiresMinutes must be 1-120 minutes.")]
    public int ExpiresMinutes { get; set; }

    [Range(1, 30, ErrorMessage = "Jwt:RefreshTokenDays must be 1-30 days.")]
    public int RefreshTokenDays { get; set; }
}
