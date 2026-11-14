using System.ComponentModel.DataAnnotations;
using ClaimsApi.Validation;

namespace ClaimsApi.Dtos;

public record UpdateClaimRequest(
    [Required, MaxLength(100)] string Title,
    [Required, NotInFuture] DateOnly? IncidentDate,
    [Range(0, double.MaxValue, MinimumIsExclusive = true)] decimal Amount,
    string? Description
);
