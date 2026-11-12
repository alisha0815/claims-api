using ClaimsApi.Dtos;

namespace ClaimsApi.Services;

public interface IClaimService
{
    Task<ClaimResponse> CreateAsync(
        int customerId,
        CreateClaimRequest request,
        CancellationToken cancellationToken = default
    );
}
