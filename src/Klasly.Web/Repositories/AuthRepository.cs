using System.Net;
using System.Net.Http.Json;
using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class AuthRepository(HttpClient http) : IAuthRepository
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login", request, ct);

        // On this call 401 means "wrong email or password" (a normal form result), not an expired login.
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;

        return await ApiResponse.ReadAsync<LoginResponse>(response, ct);
    }
}
