using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos.Auth;

public record RegisterRequest(
    [Required] [MaxLength(50)] string Username,
    [Required] [MinLength(8)] string Password
);
