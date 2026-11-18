using ClaimsApi.Auth;
using ClaimsApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> RegisterUser(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var registerResponse = await authService.RegisterAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, registerResponse);
    }
}
