using System.Net.Http.Json;
using Klasly.Core.DTOs;

namespace Klasly.Tests;

/// <summary>The in-memory API starts with the test keys and a seeded account can log in.</summary>
public class ApiFactoryTests(ApiFactory api)
{
    [Fact]
    public async Task Admin_logs_in_and_the_token_says_admin()
    {
        var admin = await api.LoginAsync("admin@klasly.test");

        var me = await admin.GetFromJsonAsync<CurrentUserResponse>("api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal("Admin", me!.Role);
    }
}
