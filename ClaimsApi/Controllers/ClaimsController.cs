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

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> GetById(
        int id,
        CancellationToken cancellationToken
    )
    {
        var claimResponse = await claimService.GetByIdAsync(id, cancellationToken);

        if (claimResponse is null)
        {
            return NotFound();
        }

        return claimResponse;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClaimResponse>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        var claims = await claimService.GetAllAsync(cancellationToken);

        return Ok(claims);
    }
}
