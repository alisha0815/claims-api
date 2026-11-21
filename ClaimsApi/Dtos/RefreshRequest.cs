using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos;

public record RefreshRequest([Required] string RefreshToken);
