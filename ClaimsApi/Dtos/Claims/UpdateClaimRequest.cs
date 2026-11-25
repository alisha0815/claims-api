using System.ComponentModel.DataAnnotations;
using ClaimsApi.Validation;

namespace ClaimsApi.Dtos.Claims;

public record UpdateClaimRequest(
    [Required, MaxLength(100)] string Title,
    [Required, NotInFuture] DateOnly? IncidentDate,
    [Range(0, double.MaxValue, MinimumIsExclusive = true)] decimal Amount,
    [MaxLength(2000)] string? Description
);
