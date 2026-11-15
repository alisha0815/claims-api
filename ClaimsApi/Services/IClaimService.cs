using ClaimsApi.Dtos;
using ClaimsApi.Models;

namespace ClaimsApi.Services;

public interface IClaimService
{
    Task<ClaimResponse> CreateAsync(
        int customerId,
        CreateClaimRequest request,
        CancellationToken cancellationToken = default
    );

    Task<ClaimResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClaimResponse>> GetAllAsync(
        ClaimStatus? status = null,
        CancellationToken cancellationToken = default
    );

    Task<UpdateClaimResult> UpdateAsync(
        int id,
        UpdateClaimRequest request,
        CancellationToken cancellationToken = default
    );

    Task<DeleteClaimResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<UpdateClaimStatusResult> UpdateStatusAsync(
        int id,
        UpdateClaimStatusRequest request,
        CancellationToken cancellationToken = default
    );
}
