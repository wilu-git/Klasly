using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Klasly.Core.Entities;

namespace Klasly.Tests;

/// <summary>
/// The vertical slice (guide §27.10 item 1) over real HTTP, the way MVC and Android call it:
/// the teacher opens a session → the API issues the QR → the student registers the phone and scans → the teacher sees it.
/// </summary>
public class VerticalSliceTests(ApiFactory api, DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Teacher_opens_student_scans_teacher_sees_present()
    {
        var (studentId, student) = await NewStudentAsync(); // a fresh student, so no other test owns the phone binding
        var classId = await TestData.NewClassAsync(db, (studentId, EnrollmentStatus.Active)); // Rosa's class
        var rosa = await api.LoginAsync("rosa.villanueva@klasly.test");

        var session = await JsonAsync(await rosa.PostAsJsonAsync($"api/classes/{classId}/sessions",
            new { startTime = DateTimeOffset.UtcNow.AddMinutes(-1), endTime = DateTimeOffset.UtcNow.AddMinutes(60), lateAfterMinutes = 15 }, Ct));
        var sessionId = session.GetProperty("sessionId").GetInt32();
        (await rosa.PostAsync($"api/sessions/{sessionId}/open", null, Ct)).EnsureSuccessStatusCode();

        var qr = await JsonAsync(await rosa.GetAsync($"api/sessions/{sessionId}/qr", Ct));
        Assert.Equal(30, qr.GetProperty("stepSeconds").GetInt32()); // QrStepSeconds (seed): a new QR every 30 s

        var deviceId = $"phone-{Guid.NewGuid():N}";
        (await student.PostAsJsonAsync("api/students/me/device", new { deviceId }, Ct)).EnsureSuccessStatusCode();
        var scan = await JsonAsync(await student.PostAsJsonAsync("api/attendance/scan",
            new { qrToken = qr.GetProperty("token").GetString(), deviceId }, Ct));
        Assert.Equal("Present", scan.GetProperty("status").GetString());

        var rows = (await JsonAsync(await rosa.GetAsync($"api/sessions/{sessionId}/attendance", Ct))).EnumerateArray();
        var row = Assert.Single(rows, r => r.GetProperty("studentId").GetInt32() == studentId);
        Assert.Equal("Present", row.GetProperty("status").GetString());
        Assert.Equal("Scan", row.GetProperty("source").GetString());
    }

    /// <summary>A new Student account made by the admin, and an HttpClient logged in as that student.</summary>
    private async Task<(int StudentId, HttpClient Client)> NewStudentAsync()
    {
        var email = $"student-{Guid.NewGuid():N}@klasly.test";
        var admin = await api.LoginAsync("admin@klasly.test");
        var account = await JsonAsync(await admin.PostAsJsonAsync("api/accounts", new
        {
            fullName = "Slice Student", email, password = ApiFactory.Password, role = "Student",
            studentNumber = $"S-{Guid.NewGuid():N}"[..20], yearLevel = 3, program = "BSCpE"
        }, Ct));

        var client = api.CreateClient();
        var login = await JsonAsync(await client.PostAsJsonAsync("api/auth/login", new { email, password = ApiFactory.Password }, Ct));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
        return (account.GetProperty("studentId").GetInt32(), client);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
