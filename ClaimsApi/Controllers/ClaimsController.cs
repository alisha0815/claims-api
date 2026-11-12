using ClaimsApi.Dtos;
using ClaimsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsApi.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponse>> PostClaim(
        CreateClaimRequest request,
        CancellationToken cancellationToken
    )
    {
        // TODO: replace with authenticated user
        var customerId = 1;

        var claimResponse = await claimService.CreateAsync(customerId, request, cancellationToken);

        return Created($"/api/claims/{claimResponse.Id}", claimResponse);
    }
}
