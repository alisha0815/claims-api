using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos;

public record RegisterRequest(
    [Required] [MaxLength(50)] string Username,
    [Required] [MaxLength(8)] string Password
);
