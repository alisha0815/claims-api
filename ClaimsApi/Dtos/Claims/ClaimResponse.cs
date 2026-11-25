using ClaimsApi.Models;

namespace ClaimsApi.Dtos.Claims;

public record ClaimResponse(
    int Id,
    int CustomerId,
    string PolicyNumber,
    string Title,
    string? Description,
    decimal Amount,
    DateOnly IncidentDate,
    ClaimStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
