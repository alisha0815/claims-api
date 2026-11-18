using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos;

public record RegisterRequest(
    [Required] [MaxLength(50)] string Username,
    [Required] [MinLength(8)] string Password
);
