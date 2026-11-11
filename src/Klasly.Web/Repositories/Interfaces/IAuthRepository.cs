using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

public interface IAuthRepository
{
    /// <summary>Returns the login result, or <c>null</c> when the email or password is wrong.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}
