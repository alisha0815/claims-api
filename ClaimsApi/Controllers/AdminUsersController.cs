using ClaimsApi.Auth;
using ClaimsApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(IAuthService authService) : ControllerBase
{
    [HttpPost("users")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> CreateHandler(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var registerResponse = await authService.CreateHandlerAsync(request, cancellationToken);

        if (registerResponse.Status == RegisterStatus.UsernameTaken)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Username is already taken."
            );
        }

        return StatusCode(StatusCodes.Status201Created, registerResponse.Response);
    }
}
