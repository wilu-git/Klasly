using System.Net;
using System.Net.Http.Json;
using Klasly.API.Services.Interfaces;
using Klasly.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Klasly.Tests;

/// <summary>
/// The right role is not enough (guide §27.5): a teacher manages only their own classes, a parent sees only linked
/// children, a student scans only classes they are enrolled in. Over real HTTP, with valid bodies so the 403 comes
/// from the ownership check, not from model validation.
/// </summary>
public class OwnershipTests(ApiFactory api, DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, object?> MarksClassEndpoints => new()
    {
        { "GET", "api/classes/{class}/sessions", null },
        { "POST", "api/classes/{class}/sessions", new { startTime = DateTimeOffset.UtcNow.AddHours(1), endTime = DateTimeOffset.UtcNow.AddHours(2) } },
        { "GET", "api/sessions/{session}", null },
        { "POST", "api/sessions/{session}/open", null },
        { "POST", "api/sessions/{session}/close", null },
        { "GET", "api/sessions/{session}/qr", null },
        { "GET", "api/sessions/{session}/attendance", null },
        { "PUT", "api/sessions/{session}/attendance/1", new { status = "Present" } },
        { "GET", "api/classes/{class}/assessments", null },
        { "POST", "api/classes/{class}/assessments", new { title = "Rosa's quiz", type = "Quiz", maxScore = 10, weightPercent = 10 } },
        { "GET", "api/assessments/{assessment}", null },
        { "GET", "api/assessments/{assessment}/grades", null },
        { "PUT", "api/assessments/{assessment}/grades", new { grades = new[] { new { studentId = 1, score = 5 } } } },
    };

    [Theory]
    [MemberData(nameof(MarksClassEndpoints))]
    public async Task Teacher_cannot_touch_another_teachers_class(string method, string url, object? body)
    {
        var (classId, sessionId, assessmentId) = await NewMarkClassAsync();
        var rosa = await api.LoginAsync("rosa.villanueva@klasly.test");

        var res = await rosa.SendAsync(new HttpRequestMessage(new HttpMethod(method),
            url.Replace("{class}", $"{classId}").Replace("{session}", $"{sessionId}").Replace("{assessment}", $"{assessmentId}"))
        { Content = body is null ? null : JsonContent.Create(body) }, Ct);

        await AssertForbiddenAsync(res, "This class belongs to another teacher.");
    }

    [Fact]
    public async Task Parent_cannot_see_a_child_who_is_not_linked()
    {
        var linda = await api.LoginAsync("linda.bautista@klasly.test"); // linked to student 5 only

        var res = await linda.GetAsync("api/parent/children/1/overview", Ct);

        await AssertForbiddenAsync(res, "You can only view your own children");
    }

    [Fact]
    public async Task Student_cannot_scan_a_class_they_are_not_enrolled_in()
    {
        var classId = await TestData.NewClassAsync(db); // no seats
        var session = await TestData.NewSessionAsync(db, classId, SessionStatus.Open, DateTime.UtcNow);
        var aira = await api.LoginAsync("aira.mendoza@klasly.test");

        var res = await aira.PostAsJsonAsync("api/attendance/scan", new { qrToken = CurrentQr(session), deviceId = "any" }, Ct);

        await AssertForbiddenAsync(res, "Not enrolled in this class");
    }

    /// <summary>A valid QR token for this session at the current step, signed with the API's own key.</summary>
    private string CurrentQr(ClassSession session)
    {
        using var scope = api.Services.CreateScope();
        var qr = scope.ServiceProvider.GetRequiredService<IQrTokenService>();
        return qr.Create(session.SessionId, qr.CurrentStep(30), session.QrNonce); // QrStepSeconds = 30 (seed)
    }

    private static async Task AssertForbiddenAsync(HttpResponseMessage res, string detail)
    {
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(detail, (await res.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Detail);
    }

    /// <summary>A class of Mark's (teacher 2) with an Open session and one assessment.</summary>
    private async Task<(int ClassId, int SessionId, int AssessmentId)> NewMarkClassAsync()
    {
        await using var context = db.CreateContext();
        var cls = new Class { SubjectId = 1, TeacherId = 2, TermId = 1, Section = $"M-{Guid.NewGuid():N}"[..30] };
        cls.Enrollments.Add(new Enrollment { StudentId = 1, Status = EnrollmentStatus.Active });
        cls.Assessments.Add(new Assessment { Title = "Mark's quiz", Type = "Quiz", MaxScore = 10, WeightPercent = 10 });
        context.Classes.Add(cls);
        await context.SaveChangesAsync(Ct);
        var session = await TestData.NewSessionAsync(db, cls.ClassId, SessionStatus.Open, DateTime.UtcNow);
        return (cls.ClassId, session.SessionId, cls.Assessments.Single().AssessmentId);
    }
}
