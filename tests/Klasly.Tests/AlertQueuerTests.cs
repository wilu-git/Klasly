using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Repositories;
using Klasly.Worker.Services;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// Queueing parent alert emails (spec assumptions 2, 3, 5, 6) on the real schema. A run queues alerts for every recent
/// absence in the database, so the class shares the "Outbox" collection with <see cref="EmailSenderTests"/> and checks
/// only the DedupKeys of its own sessions. Seed links: student 1 (Aira) and 2 → guardian 1 (Marites, user 10);
/// 3 and 4 → guardian 2 (user 11); 5 and 6 → guardian 3 (user 12).
/// </summary>
[Collection("Outbox")]
public class AlertQueuerTests(DatabaseFixture db)
{
    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    // Tue 10 Nov 2026, 9:00–10:30 AM in Manila (UTC+8).
    private static readonly DateTime SessionStart = new(2026, 11, 10, 1, 0, 0, DateTimeKind.Utc);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(AttendanceSource.Auto)]
    [InlineData(AttendanceSource.Manual)]
    public async Task A_recent_absence_queues_one_email_for_the_guardian(AttendanceSource source)
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var sessionId = await AbsenceAsync(classId, 1, source: source);

        await QueueAsync();

        var n = Assert.Single(await RowsAsync($"absent:{sessionId}:1:1"));
        Assert.Equal((10, NotificationChannel.Email, NotificationType.Absence, NotificationStatus.Pending, 0),
            (n.UserId, n.Channel, n.Type, n.Status, n.Attempts));
        Assert.Equal("Klasly: Aira Mendoza was absent from ELECT3", n.Subject);
        var section = await SectionAsync(classId);
        Assert.Contains($"ELECT3 Mobile Application Development, section {section}", n.Body);
        Assert.Contains("Tue, 10 Nov 2026, 9:00 AM–10:30 AM", n.Body);
        Assert.Contains("Marites Mendoza", n.Body);
    }

    [Fact]
    public async Task Excused_closed_old_dropped_and_past_term_absences_queue_nothing()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Dropped));
        var excused = await AbsenceAsync(classId, 1, status: AttendanceStatus.Excused);
        var closed = await AbsenceAsync(classId, 1, session: SessionStatus.Closed);
        var old = await AbsenceAsync(classId, 1, markedAt: Now.AddHours(-49));
        var dropped = await AbsenceAsync(classId, 2);
        var pastTerm = await AbsenceAsync(await NewClassInInactiveTermAsync(1), 1);

        await QueueAsync();

        Assert.Empty(await RowsAsync($"absent:{excused}:1:1", $"absent:{closed}:1:1", $"absent:{old}:1:1",
            $"absent:{dropped}:2:1", $"absent:{pastTerm}:1:1"));
    }

    [Fact]
    public async Task A_guardian_who_opted_out_gets_nothing()
    {
        var classId = await TestData.NewClassAsync(db, (4, EnrollmentStatus.Active));
        var sessionId = await AbsenceAsync(classId, 4);
        await SetReceivesAlertsAsync(studentId: 4, guardianId: 2, false);
        try
        {
            await QueueAsync();
        }
        finally
        {
            await SetReceivesAlertsAsync(studentId: 4, guardianId: 2, true);
        }

        Assert.Empty(await RowsAsync($"absent:{sessionId}:4:2"));
    }

    [Fact]
    public async Task An_inactive_guardian_account_gets_nothing()
    {
        var classId = await TestData.NewClassAsync(db, (5, EnrollmentStatus.Active));
        var sessionId = await AbsenceAsync(classId, 5);
        await SetUserActiveAsync(12, false);
        try
        {
            await QueueAsync();
        }
        finally
        {
            await SetUserActiveAsync(12, true);
        }

        Assert.Empty(await RowsAsync($"absent:{sessionId}:5:3"));
    }

    [Fact]
    public async Task Running_twice_queues_the_alert_once()
    {
        var classId = await TestData.NewClassAsync(db, (3, EnrollmentStatus.Active));
        var sessionId = await AbsenceAsync(classId, 3);

        await QueueAsync();
        await QueueAsync();

        Assert.Single(await RowsAsync($"absent:{sessionId}:3:2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Not/AZone")]
    public async Task Missing_or_unknown_TimeZoneId_fails_the_step_and_queues_nothing(string? value)
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var sessionId = await AbsenceAsync(classId, 1);
        await using var context = db.CreateContext();
        var queuer = new AlertQueuer(new NotificationRepository(context), new StubSettings(value));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => queuer.QueueAsync(Now, Ct));

        Assert.Contains("TimeZoneId", error.Message);
        Assert.Empty(await RowsAsync($"absent:{sessionId}:1:1"));
    }

    // ---------- helpers ----------

    private async Task QueueAsync()
    {
        await using var context = db.CreateContext();
        await new AlertQueuer(new NotificationRepository(context), new SystemSettingRepository(context)).QueueAsync(Now, Ct);
    }

    private async Task<List<Notification>> RowsAsync(params string[] keys)
    {
        await using var context = db.CreateContext();
        return await context.Notifications.AsNoTracking().Where(n => keys.Contains(n.DedupKey)).ToListAsync(Ct);
    }

    private async Task<string> SectionAsync(int classId)
    {
        await using var context = db.CreateContext();
        return await context.Classes.Where(c => c.ClassId == classId).Select(c => c.Section).SingleAsync(Ct);
    }

    private async Task<int> AbsenceAsync(int classId, int studentId, AttendanceStatus status = AttendanceStatus.Absent,
        SessionStatus session = SessionStatus.Finalized, DateTime? markedAt = null, AttendanceSource source = AttendanceSource.Auto)
    {
        await using var context = db.CreateContext();
        var s = new ClassSession
        {
            ClassId = classId, StartTime = SessionStart, EndTime = SessionStart.AddMinutes(90),
            QrNonce = Guid.NewGuid().ToString("N"), Status = session
        };
        s.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentId = studentId, Status = status, Source = source, MarkedAt = markedAt ?? Now.AddHours(-1),
            MarkedBy = source == AttendanceSource.Manual ? TestData.RosaUserId : null
        });
        context.ClassSessions.Add(s);
        await context.SaveChangesAsync(Ct);
        return s.SessionId;
    }

    private async Task<int> NewClassInInactiveTermAsync(int studentId)
    {
        await using var context = db.CreateContext();
        var term = new AcademicTerm
        {
            Name = $"Past-{Guid.NewGuid():N}", StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2025, 10, 31), IsActive = false
        };
        var cls = new Class { SubjectId = 1, TeacherId = 1, Term = term, Section = $"P-{Guid.NewGuid():N}"[..30] };
        cls.Enrollments.Add(new Enrollment { StudentId = studentId, Status = EnrollmentStatus.Active });
        context.Classes.Add(cls);
        await context.SaveChangesAsync(Ct);
        return cls.ClassId;
    }

    private async Task SetReceivesAlertsAsync(int studentId, int guardianId, bool value)
    {
        await using var context = db.CreateContext();
        await context.StudentGuardians.Where(sg => sg.StudentId == studentId && sg.GuardianId == guardianId)
            .ExecuteUpdateAsync(s => s.SetProperty(sg => sg.ReceivesAlerts, value), Ct);
    }

    private async Task SetUserActiveAsync(int userId, bool value)
    {
        await using var context = db.CreateContext();
        await context.Users.Where(u => u.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, value), Ct);
    }

    private sealed class StubSettings(string? timeZoneId) : ISystemSettingRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken ct) =>
            Task.FromResult(key == "TimeZoneId" ? timeZoneId : null);
    }
}
