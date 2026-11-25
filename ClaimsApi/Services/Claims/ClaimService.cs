using System.IO.Compression;
using ClaimsApi.Data;
using ClaimsApi.Dtos.Claims;
using ClaimsApi.Mappings;
using ClaimsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Services.Claims;

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

    public async Task<ClaimResponse?> GetByIdAsync(
        int id,
        int? customerId,
        CancellationToken cancellationToken = default
    )
    {
        var query = db.Claims.AsNoTracking();

        if (customerId is not null)
        {
            query = query.Where(c => c.CustomerId == customerId);
        }

        var claim = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return claim?.ToResponse();
    }

    public async Task<IReadOnlyList<ClaimResponse>> GetAllAsync(
        int? customerId,
        ClaimStatus? status = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = db.Claims.AsNoTracking();

        if (customerId is not null)
        {
            query = query.Where(c => c.CustomerId == customerId);
        }

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        var claims = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Select(c => c.ToResponse())
            .ToListAsync(cancellationToken);

        return claims;
    }

    public async Task<UpdateClaimResult> UpdateAsync(
        int id,
        int customerId,
        UpdateClaimRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var claimToUpdate = await db.Claims.FirstOrDefaultAsync(
            c => c.Id == id && c.CustomerId == customerId,
            cancellationToken
        );

        if (claimToUpdate is null)
        {
            return UpdateClaimResult.NotFound;
        }

        if (claimToUpdate.Status is not ClaimStatus.Received)
        {
            return UpdateClaimResult.NotEditable;
        }

        // Update
        var now = timeProvider.GetUtcNow().UtcDateTime;

        claimToUpdate.Title = request.Title;
        claimToUpdate.IncidentDate = request.IncidentDate!.Value;
        claimToUpdate.Amount = request.Amount;
        claimToUpdate.Description = request.Description;
        claimToUpdate.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return UpdateClaimResult.Updated;
    }

    public async Task<DeleteClaimResult> DeleteAsync(
        int id,
        int customerId,
        CancellationToken cancellationToken = default
    )
    {
        var claimToDelete = await db.Claims.FirstOrDefaultAsync(
            c => c.Id == id && c.CustomerId == customerId,
            cancellationToken
        );

        if (claimToDelete is null)
        {
            return DeleteClaimResult.NotFound;
        }

        if (claimToDelete.Status is not ClaimStatus.Received)
        {
            return DeleteClaimResult.NotDeletable;
        }

        db.Claims.Remove(claimToDelete);
        await db.SaveChangesAsync(cancellationToken);

        return DeleteClaimResult.Deleted;
    }

    public async Task<UpdateClaimStatusResult> UpdateStatusAsync(
        int id,
        UpdateClaimStatusRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (claim is null)
        {
            return UpdateClaimStatusResult.NotFound;
        }

        var currentStatus = claim.Status;
        var newStatus = request.Status!.Value;

        if (!ClaimStatusTransitions.CanTransition(currentStatus, newStatus))
        {
            return UpdateClaimStatusResult.InvalidTransition;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        claim.Status = newStatus;
        claim.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return UpdateClaimStatusResult.Updated;
    }
}
