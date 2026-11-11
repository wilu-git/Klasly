using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Klasly.Web.Services.Interfaces;

/// <summary>Either a signed-in user to store in the cookie, or the message to show on the login form.</summary>
public record LoginResult(ClaimsPrincipal? Principal, AuthenticationProperties? Properties, string? Error);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct);
}
