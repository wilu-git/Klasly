using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Klasly.Tests;

/// <summary>
/// The scan checks of guide §27.8 in their order, each with its exact status and message, over real HTTP.
/// Check 1 (role) is in RoleAccessTests. Francis (student 6) is used only here, so his registered phone is ours.
/// </summary>
public class ScanTests(ApiFactory api, DatabaseFixture db)
{
    private const int StudentId = 6;          // Francis Nuqui, user 9
    private const string Phone = "scan-tests-phone";
    private const int StepSeconds = 30;       // QrStepSeconds (seed)
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Bad_signature_is_400_invalid_qr()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow);
        var token = await QrAsync(session, 0);

        await AssertProblemAsync(await ScanAsync(token[..^2] + "xx"), HttpStatusCode.BadRequest, "Invalid QR code");
    }

    [Fact]
    public async Task Qr_two_steps_old_is_400_expired()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow);

        await AssertProblemAsync(await ScanAsync(await QrAsync(session, -2)), HttpStatusCode.BadRequest, "QR expired – rescan");
    }

    [Fact]
    public async Task Qr_one_step_old_is_still_accepted()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow);

        var res = await ScanAsync(await QrAsync(session, -1));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Session_that_is_not_open_is_409_closed()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow, SessionStatus.Scheduled);

        await AssertProblemAsync(await ScanAsync(await QrAsync(session, 0)), HttpStatusCode.Conflict, "Session is closed");
    }

    [Fact]
    public async Task Dropped_student_is_403_not_enrolled()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow, enrollment: EnrollmentStatus.Dropped);

        await AssertProblemAsync(await ScanAsync(await QrAsync(session, 0)), HttpStatusCode.Forbidden, "Not enrolled in this class");
    }

    [Fact]
    public async Task Another_phone_is_403_unregistered_device()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow);

        await AssertProblemAsync(await ScanAsync(await QrAsync(session, 0), "someone-elses-phone"),
            HttpStatusCode.Forbidden, "Unregistered device");
    }

    [Fact]
    public async Task Second_scan_is_409_already_checked_in()
    {
        var session = await OpenSessionAsync(DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.OK, (await ScanAsync(await QrAsync(session, 0))).StatusCode);

        await AssertProblemAsync(await ScanAsync(await QrAsync(session, 0)), HttpStatusCode.Conflict, "Already checked in");
    }

    [Theory]
    [InlineData(-14, AttendanceStatus.Present)] // started 14 min ago, late after 15 → inside the cut-off
    [InlineData(-16, AttendanceStatus.Late)]    // 1 min past the cut-off
    public async Task Scan_at_the_late_cut_off(int startedMinutesAgo, AttendanceStatus expected)
    {
        var session = await OpenSessionAsync(DateTime.UtcNow.AddMinutes(startedMinutesAgo));

        var res = await ScanAsync(await QrAsync(session, 0));

        Assert.Equal(expected, (await res.Content.ReadFromJsonAsync<ScanResponse>(Json, Ct))!.Status);
        await using var context = db.CreateContext();
        var record = await context.AttendanceRecords.SingleAsync(r => r.SessionId == session.SessionId, Ct);
        Assert.Equal((expected, AttendanceSource.Scan), (record.Status, record.Source));
        Assert.NotNull(record.ScannedAt);
    }

    private async Task<HttpResponseMessage> ScanAsync(string token, string deviceId = Phone) =>
        await (await api.LoginAsync("francis.nuqui@klasly.test"))
            .PostAsJsonAsync("api/attendance/scan", new ScanRequest { QrToken = token, DeviceId = deviceId }, Ct);

    /// <summary>A token for the current step + offset, signed with the API's own key.</summary>
    private async Task<string> QrAsync(ClassSession session, int stepOffset)
    {
        // Don't sign in the last 3 s of a step: the server could see the next step and turn −1 into −2.
        var intoStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % StepSeconds;
        if (intoStep >= StepSeconds - 3) await Task.Delay(TimeSpan.FromSeconds(StepSeconds - intoStep + 0.2), Ct);

        using var scope = api.Services.CreateScope();
        var qr = scope.ServiceProvider.GetRequiredService<IQrTokenService>();
        return qr.Create(session.SessionId, qr.CurrentStep(StepSeconds) + stepOffset, session.QrNonce);
    }

    /// <summary>A new class with Francis seated, his phone registered, and one session starting at <paramref name="startUtc"/>.</summary>
    private async Task<ClassSession> OpenSessionAsync(
        DateTime startUtc, SessionStatus status = SessionStatus.Open, EnrollmentStatus enrollment = EnrollmentStatus.Active)
    {
        await using (var context = db.CreateContext())
            await context.Users.Where(u => u.UserId == 9).ExecuteUpdateAsync(u => u.SetProperty(x => x.RegisteredDeviceId, Phone), Ct);
        var classId = await TestData.NewClassAsync(db, (StudentId, enrollment));
        return await TestData.NewSessionAsync(db, classId, status, startUtc);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string detail)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(detail, (await res.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }
}
