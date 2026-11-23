using System.Diagnostics;
using System.Security.Claims;
using ClaimsApi.Dtos;
using ClaimsApi.Models;
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
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        var customerId = int.Parse(userIdValue);

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
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        var customerId = int.Parse(userIdValue);

        var claimResponse = await claimService.GetByIdAsync(id, customerId, cancellationToken);

        if (claimResponse is null)
        {
            return NotFound();
        }

        return claimResponse;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ClaimResponse>>> GetAll(
        [FromQuery] ClaimStatus? status,
        CancellationToken cancellationToken
    )
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        var customerId = int.Parse(userIdValue);

        var claims = await claimService.GetAllAsync(customerId, status, cancellationToken);

        return Ok(claims);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateById(
        int id,
        UpdateClaimRequest request,
        CancellationToken cancellationToken
    )
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        var customerId = int.Parse(userIdValue);
        var result = await claimService.UpdateAsync(id, customerId, request, cancellationToken);

        return result switch
        {
            UpdateClaimResult.Updated => NoContent(),
            UpdateClaimResult.NotFound => NotFound(),
            UpdateClaimResult.NotEditable => Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "Only claims in Received status can be updated."
            ),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteById(int id, CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id");

        var customerId = int.Parse(userIdValue);

        var result = await claimService.DeleteAsync(id, customerId, cancellationToken);

        return result switch
        {
            DeleteClaimResult.Deleted => NoContent(),
            DeleteClaimResult.NotFound => NotFound(),
            DeleteClaimResult.NotDeletable => Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "Only claims in Received status can be deleted."
            ),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateStatusById(
        int id,
        UpdateClaimStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await claimService.UpdateStatusAsync(id, request, cancellationToken);

        return result switch
        {
            UpdateClaimStatusResult.Updated => NoContent(),
            UpdateClaimStatusResult.NotFound => NotFound(),
            UpdateClaimStatusResult.InvalidTransition => Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "This status change is not allowed. Allowed: Received -> InReview -> Approved or Rejected"
            ),
            _ => throw new UnreachableException(),
        };
    }
}
