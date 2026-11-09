using System.Security.Claims;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Klasly.Web.Services;

public class AuthService(IAuthRepository auth) : IAuthService
{
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var login = await auth.LoginAsync(new LoginRequest(email, password), ct);
        if (login is null) return new LoginResult(null, null, "Invalid email or password.");

        // The student screens are Android only (CLAUDE.md §3.6).
        if (login.Role == "Student") return new LoginResult(null, null, "Students use the Klasly mobile app.");

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, login.UserId.ToString()),
                new Claim(ClaimTypes.Name, login.FullName),
                new Claim(ClaimTypes.Role, login.Role),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        // The login ends exactly when the JWT ends; not persistent, so closing the browser also logs out.
        var properties = new AuthenticationProperties { ExpiresUtc = login.ExpiresAtUtc, IsPersistent = false };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = login.Token }]);

        return new LoginResult(new ClaimsPrincipal(identity), properties, null);
    }
}
