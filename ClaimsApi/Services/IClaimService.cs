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
}
