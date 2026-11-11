using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Returns a signed JWT, or null when the email, password or account status is not valid.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}
