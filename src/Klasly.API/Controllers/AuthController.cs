using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Exchanges email + password for a JWT that carries the user's role.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await authService.LoginAsync(request, ct);

        // One message for unknown email, wrong password and deactivated account, so emails can't be probed.
        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid email or password.")
            : Ok(response);
    }

    /// <summary>Returns the identity inside the caller's token. Proves the JWT is accepted.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me() => new CurrentUserResponse(
        int.Parse(User.FindFirstValue("sub")!),
        User.FindFirstValue("email")!,
        User.FindFirstValue("name")!,
        User.FindFirstValue("role")!);
}
