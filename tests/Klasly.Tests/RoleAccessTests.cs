using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Klasly.Tests;

/// <summary>
/// Every API endpoint, found from the app itself (so new endpoints are covered automatically), against every role
/// (guide §27.5: authorization is enforced in the Web API; CLAUDE.md §3.3: 401 no/bad token, 403 wrong role).
/// </summary>
public partial class RoleAccessTests(ApiFactory api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // One seeded account per role (KlaslySeed). Rosa owns class 1, Marites is linked to students 1 and 2.
    private static readonly Dictionary<string, string> Accounts = new()
    {
        ["Admin"] = "admin@klasly.test",
        ["Teacher"] = "rosa.villanueva@klasly.test",
        ["Student"] = "aira.mendoza@klasly.test",
        ["Parent"] = "marites.mendoza@klasly.test",
    };

    private static readonly Dictionary<string, string> PolicyRole = new()
    {
        ["AdminOnly"] = "Admin", ["TeacherOnly"] = "Teacher", ["StudentOnly"] = "Student", ["ParentOnly"] = "Parent",
    };

    private sealed record Api(string Method, string Url, string? Role, bool Anonymous)
    {
        public override string ToString() => $"{Method} /{Url}";
    }

    private List<Api> Endpoints()
    {
        var list = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .SelectMany(e =>
            {
                var url = RouteValue().Replace(e.RoutePattern.RawText!.TrimStart('/'), "1"); // every {id} → 1
                var policy = e.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).LastOrDefault(p => p is not null);
                var anonymous = e.Metadata.GetMetadata<IAllowAnonymous>() is not null;
                return e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods
                    .Select(m => new Api(m, url, policy is null ? null : PolicyRole[policy], anonymous));
            })
            .ToList();

        Assert.True(list.Count > 40, $"Only {list.Count} endpoints found; the endpoint discovery is broken.");
        return list;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, Api e) =>
        client.SendAsync(new HttpRequestMessage(new HttpMethod(e.Method), e.Url)
        {
            Content = e.Method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        }, Ct);

    [Fact]
    public async Task Every_endpoint_except_login_refuses_a_missing_token_with_401()
    {
        var anonymous = api.CreateClient();
        var failures = new List<string>();

        foreach (var e in Endpoints().Where(e => !e.Anonymous))
        {
            var res = await SendAsync(anonymous, e);
            if (res.StatusCode != HttpStatusCode.Unauthorized) failures.Add($"{e} → {(int)res.StatusCode}");
        }

        Assert.Empty(failures);
        Assert.Equal(["POST /api/auth/login"], Endpoints().Where(e => e.Anonymous).Select(e => e.ToString()));
    }

    [Fact]
    public void Every_endpoint_has_a_role_policy_except_login_me_and_scan()
    {
        // Scan has no policy on purpose: a non-student must get 401 (§27.8 check 1), so the service checks the role.
        Assert.Equal(
            ["GET /api/auth/me", "POST /api/attendance/scan", "POST /api/auth/login"],
            Endpoints().Where(e => e.Role is null).Select(e => e.ToString()).Order());
    }

    [Fact]
    public async Task Every_role_endpoint_refuses_each_wrong_role_with_403()
    {
        var failures = new List<string>();

        foreach (var e in Endpoints().Where(e => e.Role is not null))
            foreach (var (role, email) in Accounts.Where(a => a.Key != e.Role))
            {
                var res = await SendAsync(await api.LoginAsync(email), e);
                if (res.StatusCode != HttpStatusCode.Forbidden) failures.Add($"{e} as {role} → {(int)res.StatusCode}");
            }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task Every_role_endpoint_lets_its_own_role_through()
    {
        var failures = new List<string>();

        // GETs only: a right-role DELETE or POST would really run and could soft-delete seeded rows other tests use.
        foreach (var e in Endpoints().Where(e => e.Role is not null && e.Method == "GET"))
        {
            var res = await SendAsync(await api.LoginAsync(Accounts[e.Role!]), e);
            // A policy refusal is a 401 or an empty 403; an ownership 403 (with a message) means the policy let it through.
            var policyRefused = res.StatusCode == HttpStatusCode.Unauthorized
                || (res.StatusCode == HttpStatusCode.Forbidden && (await res.Content.ReadAsStringAsync(Ct)).Length == 0);
            if (policyRefused) failures.Add($"{e} as {e.Role} → {(int)res.StatusCode}");
        }

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Teacher")]
    [InlineData("Parent")]
    public async Task Scan_by_a_non_student_is_401_session_expired(string role)
    {
        var client = await api.LoginAsync(Accounts[role]);

        var res = await client.PostAsJsonAsync("api/attendance/scan", new { qrToken = "1.1.x", deviceId = "d" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("Session expired – please log in", (await res.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteValue();
}
