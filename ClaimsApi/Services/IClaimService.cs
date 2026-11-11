using ClaimsApi.Dtos;

namespace ClaimsApi.Services;

public interface IClaimService
{
    Task<ClaimResponse> CreateAsync(
        CreateClaimRequest request,
        CancellationToken cancellationToken = default
    );
}
