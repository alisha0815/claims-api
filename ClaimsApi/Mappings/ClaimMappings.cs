using ClaimsApi.Dtos.Claims;
using ClaimsApi.Models;

namespace ClaimsApi.Mappings;

public static class ClaimMappings
{
    public static ClaimResponse ToResponse(this Claim claim)
    {
        return new ClaimResponse(
            Id: claim.Id,
            CustomerId: claim.CustomerId,
            PolicyNumber: claim.PolicyNumber,
            Title: claim.Title,
            Description: claim.Description,
            Amount: claim.Amount,
            IncidentDate: claim.IncidentDate,
            Status: claim.Status,
            CreatedAt: claim.CreatedAt,
            UpdatedAt: claim.UpdatedAt
        );
    }
}
