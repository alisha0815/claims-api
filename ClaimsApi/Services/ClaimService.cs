using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Mappings;
using ClaimsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Services;

public class ClaimService(AppDbContext db, TimeProvider timeProvider) : IClaimService
{
    public async Task<ClaimResponse> CreateAsync(
        int customerId,
        CreateClaimRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var claim = new Claim
        {
            CustomerId = customerId,
            PolicyNumber = request.PolicyNumber,
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            IncidentDate = request.IncidentDate!.Value,

            Status = ClaimStatus.Received,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Add(claim);

        await db.SaveChangesAsync(cancellationToken);

        return claim.ToResponse();
    }

    public async Task<ClaimResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var claim = await db
            .Claims.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return claim?.ToResponse();
    }
}
