using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos.Auth;

public record RefreshRequest([Required] string RefreshToken);
