using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Dtos;

public record CreateClaimRequest(
    [Required, MaxLength(50)] string PolicyNumber,
    [Required, MaxLength(100)] string Title,
    [Required] DateOnly? IncidentDate,
    [Range(0, double.MaxValue, MinimumIsExclusive = true)] decimal Amount,
    string? Description
);
