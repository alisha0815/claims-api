using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos;

public record LoginRequest([Required] string Username, [Required] string Password);
