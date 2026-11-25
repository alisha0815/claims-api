using System.Security.Claims;
using ClaimsApi.Auth;
using ClaimsApi.Dtos.Auth;
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> RegisterUser(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var registerResponse = await authService.RegisterAsync(request, cancellationToken);

        if (registerResponse.Status == RegisterStatus.UsernameTaken)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Username is already taken."
            );
        }

        return StatusCode(StatusCodes.Status201Created, registerResponse.Response);
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result.Status == LoginStatus.InvalidCredentials)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid username or password."
            );
        }

        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await authService.RefreshAsync(request, cancellationToken);

        if (result.Status == RefreshStatus.Invalid)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid or expired refresh token."
            );
        }

        return Ok(result.Response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AllowAnonymous]
    public async Task<ActionResult> Logout(
        RefreshRequest request,
        CancellationToken cancellationToken
    )
    {
        await authService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    public ActionResult<MeResponse> GetMe()
    {
        var idValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user Id claim");
        var id = int.Parse(idValue);

        var username =
            User.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("Missing user name claim");

        var role =
            User.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Missing user role claim");

        var response = new MeResponse(id, username, role);
        return Ok(response);
    }
}
