using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos.Auth;

public record LoginRequest([Required] string Username, [Required] string Password);
