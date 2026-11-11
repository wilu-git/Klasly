using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Tests;

/// <summary>
/// The admin rules over real HTTP (guide §27.9 FR2, §27.7 soft delete): drops and deactivations keep the row,
/// re-enrolling reuses it, and an admin can't lock themselves out.
/// </summary>
public class AdminRulesTests(ApiFactory api, DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dropping_keeps_the_row_and_reenrolling_reuses_it()
    {
        var classId = await TestData.NewClassAsync(db); // no seats yet
        var admin = await api.LoginAsync("admin@klasly.test");

        var enrolled = await admin.PostAsJsonAsync("api/enrollments", new { classId, studentId = 4 }, Ct);
        Assert.Equal(HttpStatusCode.Created, enrolled.StatusCode);
        var enrollmentId = (await JsonAsync(enrolled)).GetProperty("enrollmentId").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/enrollments/{enrollmentId}", Ct)).StatusCode);
        var row = Assert.Single((await JsonAsync(await admin.GetAsync($"api/classes/{classId}/enrollments", Ct))).EnumerateArray());
        Assert.Equal("Dropped", row.GetProperty("status").GetString());

        var again = await admin.PostAsJsonAsync("api/enrollments", new { classId, studentId = 4 }, Ct);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode); // 200, not 201: the old row is back
        var back = await JsonAsync(again);
        Assert.Equal(enrollmentId, back.GetProperty("enrollmentId").GetInt32());
        Assert.Equal("Active", back.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deactivated_account_stays_listed_as_inactive()
    {
        var admin = await api.LoginAsync("admin@klasly.test");
        var created = await admin.PostAsJsonAsync("api/accounts",
            new { fullName = "Soft Delete", email = $"soft-{Guid.NewGuid():N}@klasly.test", password = ApiFactory.Password, role = "Parent" }, Ct);
        var userId = (await JsonAsync(created)).GetProperty("userId").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/accounts/{userId}", Ct)).StatusCode);
        var account = await JsonAsync(await admin.GetAsync($"api/accounts/{userId}", Ct));

        Assert.False(account.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Admin_cannot_deactivate_their_own_account()
    {
        var admin = await api.LoginAsync("admin@klasly.test"); // user 1

        var res = await admin.DeleteAsync("api/accounts/1", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("You cannot deactivate your own account.", (await res.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}

/// <summary>
/// Activating a term switches the seeded active term off for a moment, which other tests read,
/// so this runs alone, after the parallel tests.
/// </summary>
[CollectionDefinition(nameof(ActiveTermCollection), DisableParallelization = true)]
public class ActiveTermCollection;

[Collection(nameof(ActiveTermCollection))]
public class TermActivationTests(ApiFactory api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Activating_a_term_leaves_it_the_only_active_one()
    {
        var admin = await api.LoginAsync("admin@klasly.test");
        var created = await admin.PostAsJsonAsync("api/terms",
            new { name = $"Test {Guid.NewGuid():N}", startDate = "2027-01-04", endDate = "2027-05-28" }, Ct);
        created.EnsureSuccessStatusCode();
        var termId = (await created.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("termId").GetInt32();

        try
        {
            (await admin.PostAsync($"api/terms/{termId}/activate", null, Ct)).EnsureSuccessStatusCode();
            var terms = (await admin.GetFromJsonAsync<JsonElement>("api/terms", Ct)).EnumerateArray().ToList();

            var active = Assert.Single(terms, t => t.GetProperty("isActive").GetBoolean());
            Assert.Equal(termId, active.GetProperty("termId").GetInt32());
        }
        finally
        {
            (await admin.PostAsync("api/terms/1/activate", null, Ct)).EnsureSuccessStatusCode(); // the seeded term again
        }
    }
}
