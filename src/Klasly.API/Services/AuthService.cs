using System.Security.Claims;
using System.Text;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Klasly.API.Services;

/// <summary>Checks credentials with the Identity password hasher and issues the JWT (guide Section 27.5).</summary>
public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null || !user.IsActive) return null;

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        return CreateToken(user);
    }

    private LoginResponse CreateToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpiryMinutes", 60));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        // Only identity and role go in the token: never the hash, device id or FCM token.
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim("role", user.Role.ToString())
            ])
        });

        return new LoginResponse(token, expiresAtUtc, user.UserId, user.FullName, user.Role.ToString());
    }
}
