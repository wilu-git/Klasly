using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

[assembly: AssemblyFixture(typeof(Klasly.Tests.ApiFactory))]

namespace Klasly.Tests;

/// <summary>
/// The real Web API, in memory, on the Klasly_Tests database: real JWT, policies, services and status codes.
/// Fake test-only keys; the "Testing" environment keeps the developer's user-secrets and Swagger out.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Klasly@123"; // the seeded test password (KlaslySeed), fictional accounts only

    private readonly ConcurrentDictionary<string, Task<HttpClient>> clients = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting, not ConfigureAppConfiguration: Program.cs reads the keys before Build().
        builder.UseSetting("ConnectionStrings:Klasly", DatabaseFixture.ConnectionString);
        builder.UseSetting("Jwt:Key", "test-only-jwt-key-0123456789-abcdefghij");
        builder.UseSetting("Qr:SigningKey", "test-only-qr-key-0123456789-abcdefghijk");
        builder.UseSetting("Fcm:ServiceAccountPath", "");
    }

    /// <summary>An HttpClient that sends the bearer token of this seeded account. One per email for the whole run.</summary>
    public Task<HttpClient> LoginAsync(string email) => clients.GetOrAdd(email, async e =>
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = e, Password = Password });
        res.EnsureSuccessStatusCode();
        var login = (await res.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    });
}
