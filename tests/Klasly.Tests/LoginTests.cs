using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Tests;

/// <summary>
/// Login and deactivation over real HTTP (guide §27.5, §27.7 soft delete): one message for every failed login,
/// and a deactivated account loses access at once, not when its token expires.
/// </summary>
public class LoginTests(ApiFactory api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Wrong_password_is_401_invalid_email_or_password()
    {
        var res = await LoginAsync("aira.mendoza@klasly.test", "Wrong-password-1");

        await AssertProblemAsync(res, HttpStatusCode.Unauthorized, "Invalid email or password.");
    }

    [Fact]
    public async Task Deactivated_account_cannot_log_in_and_gets_the_same_message()
    {
        var (userId, email) = await NewParentAsync();
        var admin = await api.LoginAsync("admin@klasly.test");
        (await admin.DeleteAsync($"api/accounts/{userId}", Ct)).EnsureSuccessStatusCode();

        var res = await LoginAsync(email, ApiFactory.Password);

        await AssertProblemAsync(res, HttpStatusCode.Unauthorized, "Invalid email or password.");
    }

    [Fact]
    public async Task Deactivated_account_loses_access_at_once_with_its_old_token()
    {
        var (userId, email) = await NewParentAsync();
        var parent = api.CreateClient();
        var login = (await (await LoginAsync(email, ApiFactory.Password)).Content.ReadFromJsonAsync<LoginResponse>(Ct))!;
        parent.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        (await parent.GetAsync("api/parent/children", Ct)).EnsureSuccessStatusCode(); // the token works

        var admin = await api.LoginAsync("admin@klasly.test");
        (await admin.DeleteAsync($"api/accounts/{userId}", Ct)).EnsureSuccessStatusCode();
        var res = await parent.GetAsync("api/parent/children", Ct);

        await AssertProblemAsync(res, HttpStatusCode.Unauthorized, "Session expired – please log in");
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        api.CreateClient().PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Password = password }, Ct);

    /// <summary>A new Parent account made by the admin, so deactivating it never touches a seeded user.</summary>
    private async Task<(int UserId, string Email)> NewParentAsync()
    {
        var email = $"parent-{Guid.NewGuid():N}@klasly.test";
        var admin = await api.LoginAsync("admin@klasly.test");
        var res = await admin.PostAsJsonAsync("api/accounts",
            new { fullName = "Test Parent", email, password = ApiFactory.Password, role = "Parent" }, Ct);
        res.EnsureSuccessStatusCode();
        return ((await res.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("userId").GetInt32(), email);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string detail)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(detail, (await res.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }
}
