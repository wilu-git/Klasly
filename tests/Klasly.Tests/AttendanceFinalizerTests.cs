using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;
using Klasly.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Klasly.Tests;

/// <summary>
/// The Worker's attendance rules on the real schema. Each test makes its own class, so its rows never clash with another test's.
/// The finalizer works on every session in the database, so tests check their own rows, not global counts.
/// </summary>
public class AttendanceFinalizerTests(DatabaseFixture db)
{
    // The time columns are datetime2(0), so "now" has whole seconds and round-trips exactly.
    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Task 3: close ended sessions, cancel never-opened ones ----------

    [Fact]
    public async Task Ended_open_session_is_closed_and_ended_scheduled_session_is_cancelled()
    {
        var classId = await NewClassAsync();
        var open = await NewSessionAsync(classId, SessionStatus.Open, end: Now.AddMinutes(-1));
        var scheduled = await NewSessionAsync(classId, SessionStatus.Scheduled, end: Now.AddMinutes(-1));

        await RunCloseAsync();

        var openAfter = await GetSessionAsync(open);
        Assert.Equal(SessionStatus.Closed, openAfter.Status);
        Assert.Equal(Now, openAfter.ClosedAt);

        var scheduledAfter = await GetSessionAsync(scheduled);
        Assert.Equal(SessionStatus.Cancelled, scheduledAfter.Status);
        Assert.Null(scheduledAfter.ClosedAt);
    }

    [Fact]
    public async Task Session_ending_exactly_now_counts_as_ended()
    {
        var classId = await NewClassAsync();
        var open = await NewSessionAsync(classId, SessionStatus.Open, end: Now);

        await RunCloseAsync();

        Assert.Equal(SessionStatus.Closed, (await GetSessionAsync(open)).Status);
    }

    [Fact]
    public async Task Sessions_not_yet_ended_or_already_done_are_untouched()
    {
        var classId = await NewClassAsync();
        var openLater = await NewSessionAsync(classId, SessionStatus.Open, end: Now.AddMinutes(1));
        var scheduledLater = await NewSessionAsync(classId, SessionStatus.Scheduled, end: Now.AddMinutes(1));
        var closedAt = Now.AddHours(-1);
        var closed = await NewSessionAsync(classId, SessionStatus.Closed, end: Now.AddMinutes(-1), closedAt);
        var finalized = await NewSessionAsync(classId, SessionStatus.Finalized, end: Now.AddMinutes(-1), closedAt);
        var cancelled = await NewSessionAsync(classId, SessionStatus.Cancelled, end: Now.AddMinutes(-1));

        // Only the close step: the Closed session must not be finalized here.
        await using (var context = db.CreateContext())
            await NewFinalizer(context).CloseEndedSessionsAsync(Now, Ct);

        Assert.Equal(SessionStatus.Open, (await GetSessionAsync(openLater)).Status);
        Assert.Equal(SessionStatus.Scheduled, (await GetSessionAsync(scheduledLater)).Status);
        Assert.Equal((SessionStatus.Closed, closedAt), Snapshot(await GetSessionAsync(closed)));
        Assert.Equal((SessionStatus.Finalized, closedAt), Snapshot(await GetSessionAsync(finalized)));
        Assert.Equal(SessionStatus.Cancelled, (await GetSessionAsync(cancelled)).Status);
    }

    [Fact]
    public async Task Running_close_twice_changes_nothing_the_second_time()
    {
        var classId = await NewClassAsync();
        var open = await NewSessionAsync(classId, SessionStatus.Open, end: Now.AddMinutes(-1));
        await RunCloseAsync();

        await using var context = db.CreateContext();
        // Same "now": a later time would also close another test's not-yet-ended sessions.
        var (closedCount, cancelledCount) = await NewFinalizer(context).CloseEndedSessionsAsync(Now, Ct);

        Assert.Equal((0, 0), (closedCount, cancelledCount));
        Assert.Equal((SessionStatus.Closed, Now), Snapshot(await GetSessionAsync(open)));
    }

    // ---------- Task 4: mark non-scanners Absent and finalize ----------

    [Fact]
    public async Task Non_scanners_get_Absent_and_the_session_is_finalized()
    {
        var classId = await NewClassAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active), (3, EnrollmentStatus.Active));
        var session = await NewClosedSessionAsync(classId);
        await AddRecordAsync(session, 1, AttendanceStatus.Present, AttendanceSource.Scan, scannedAt: Now.AddMinutes(-50));

        await RunFinalizeAsync();

        Assert.Equal(SessionStatus.Finalized, (await GetSessionAsync(session)).Status);
        var records = await GetRecordsAsync(session);
        Assert.Equal(
            [(1, AttendanceStatus.Present, AttendanceSource.Scan), (2, AttendanceStatus.Absent, AttendanceSource.Auto), (3, AttendanceStatus.Absent, AttendanceSource.Auto)],
            records.Select(r => (r.StudentId, r.Status, r.Source)));
        Assert.Equal(Now.AddMinutes(-50), records[0].ScannedAt);
        Assert.All(records.Skip(1), r =>
        {
            Assert.Equal(Now, r.MarkedAt);
            Assert.Null(r.ScannedAt);
            Assert.Null(r.MarkedBy);
        });
    }

    [Fact]
    public async Task Dropped_student_gets_no_row()
    {
        var classId = await NewClassAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Dropped));
        var session = await NewClosedSessionAsync(classId);

        await RunFinalizeAsync();

        Assert.Equal([1], (await GetRecordsAsync(session)).Select(r => r.StudentId));
    }

    [Fact]
    public async Task Manual_correction_is_kept()
    {
        var classId = await NewClassAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var session = await NewClosedSessionAsync(classId);
        await AddRecordAsync(session, 1, AttendanceStatus.Excused, AttendanceSource.Manual, markedBy: 2);

        await RunFinalizeAsync();

        var records = await GetRecordsAsync(session);
        Assert.Equal(
            [(1, AttendanceStatus.Excused, AttendanceSource.Manual, (int?)2), (2, AttendanceStatus.Absent, AttendanceSource.Auto, null)],
            records.Select(r => (r.StudentId, r.Status, r.Source, r.MarkedBy)));
    }

    [Fact]
    public async Task Session_with_no_enrollments_is_still_finalized()
    {
        var session = await NewClosedSessionAsync(await NewClassAsync());

        await RunFinalizeAsync();

        Assert.Equal(SessionStatus.Finalized, (await GetSessionAsync(session)).Status);
        Assert.Empty(await GetRecordsAsync(session));
    }

    [Fact]
    public async Task Running_finalize_twice_adds_nothing_the_second_time()
    {
        var classId = await NewClassAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var session = await NewClosedSessionAsync(classId);
        await RunFinalizeAsync();
        var first = await GetRecordsAsync(session);

        await RunFinalizeAsync();

        var second = await GetRecordsAsync(session);
        Assert.Equal(
            first.Select(r => (r.AttendanceId, r.Status, r.Source, r.MarkedAt)),
            second.Select(r => (r.AttendanceId, r.Status, r.Source, r.MarkedAt)));
    }

    [Fact]
    public async Task Row_added_while_finalizing_stops_the_tick_and_the_next_run_keeps_it()
    {
        await RunFinalizeAsync(); // finish other tests' Closed sessions, so this one is the only one left
        var classId = await NewClassAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var session = await NewClosedSessionAsync(classId);

        // A teacher's correction for student 2 lands after the Worker read the roster, before it saves.
        await using (var context = db.CreateContext())
        {
            var racing = new RacingAttendanceRepository(
                new AttendanceRepository(context),
                () => AddRecordAsync(session, 2, AttendanceStatus.Excused, AttendanceSource.Manual, markedBy: 2));
            var finalizer = new AttendanceFinalizer(
                new SessionRepository(context), racing, new EnrollmentRepository(context), NullLogger<AttendanceFinalizer>.Instance);

            Assert.Equal(0, await finalizer.MarkAbsentAndFinalizeAsync(Now, Ct));
        }

        Assert.Equal(SessionStatus.Closed, (await GetSessionAsync(session)).Status);
        Assert.Equal([2], (await GetRecordsAsync(session)).Select(r => r.StudentId)); // the Worker's batch was rolled back

        await RunFinalizeAsync();

        Assert.Equal(SessionStatus.Finalized, (await GetSessionAsync(session)).Status);
        Assert.Equal(
            [(1, AttendanceStatus.Absent, AttendanceSource.Auto), (2, AttendanceStatus.Excused, AttendanceSource.Manual)],
            (await GetRecordsAsync(session)).Select(r => (r.StudentId, r.Status, r.Source)));
    }

    /// <summary>Runs <paramref name="beforeFirstSave"/> once, just before the first save, to simulate a concurrent write.</summary>
    private sealed class RacingAttendanceRepository(AttendanceRepository inner, Func<Task> beforeFirstSave) : IAttendanceRepository
    {
        private bool raced;

        public async Task<bool> TrySaveAsync(CancellationToken ct)
        {
            if (!raced)
            {
                raced = true;
                await beforeFirstSave();
            }
            return await inner.TrySaveAsync(ct);
        }

        public Task<AttendanceRecord?> GetAsync(int sessionId, int studentId, CancellationToken ct) => inner.GetAsync(sessionId, studentId, ct);
        public Task<bool> ExistsAsync(int sessionId, int studentId, CancellationToken ct) => inner.ExistsAsync(sessionId, studentId, ct);
        public Task<List<AttendanceRecord>> ListBySessionAsync(int sessionId, CancellationToken ct) => inner.ListBySessionAsync(sessionId, ct);
        public Task<bool> TryAddAsync(AttendanceRecord record, CancellationToken ct) => inner.TryAddAsync(record, ct);
        public void Add(AttendanceRecord record) => inner.Add(record);
    }

    // ---------- helpers ----------

    private async Task RunFinalizeAsync()
    {
        await using var context = db.CreateContext();
        await NewFinalizer(context).MarkAbsentAndFinalizeAsync(Now, Ct);
    }

    private Task<int> NewClosedSessionAsync(int classId) =>
        NewSessionAsync(classId, SessionStatus.Closed, end: Now.AddMinutes(-1), closedAt: Now.AddMinutes(-1));

    private async Task AddRecordAsync(
        int sessionId, int studentId, AttendanceStatus status, AttendanceSource source, DateTime? scannedAt = null, int? markedBy = null)
    {
        await using var context = db.CreateContext();
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            SessionId = sessionId,
            StudentId = studentId,
            Status = status,
            Source = source,
            ScannedAt = scannedAt,
            MarkedAt = Now.AddMinutes(-30),
            MarkedBy = markedBy
        });
        await context.SaveChangesAsync(Ct);
    }

    private async Task<List<AttendanceRecord>> GetRecordsAsync(int sessionId)
    {
        await using var context = db.CreateContext();
        return await context.AttendanceRecords.AsNoTracking()
            .Where(a => a.SessionId == sessionId).OrderBy(a => a.StudentId).ToListAsync(Ct);
    }

    private static AttendanceFinalizer NewFinalizer(KlaslyDbContext context) => new(
        new SessionRepository(context),
        new AttendanceRepository(context),
        new EnrollmentRepository(context),
        NullLogger<AttendanceFinalizer>.Instance);

    private async Task RunCloseAsync()
    {
        await using var context = db.CreateContext();
        await NewFinalizer(context).CloseEndedSessionsAsync(Now, Ct);
    }

    private static (SessionStatus, DateTime?) Snapshot(ClassSession s) => (s.Status, s.ClosedAt);

    private Task<int> NewClassAsync(params (int StudentId, EnrollmentStatus Status)[] seats) => TestData.NewClassAsync(db, seats);

    private async Task<int> NewSessionAsync(int classId, SessionStatus status, DateTime end, DateTime? closedAt = null)
    {
        await using var context = db.CreateContext();
        var session = new ClassSession
        {
            ClassId = classId,
            StartTime = end.AddHours(-1),
            EndTime = end,
            QrNonce = Guid.NewGuid().ToString("N"),
            Status = status,
            ClosedAt = closedAt
        };
        context.ClassSessions.Add(session);
        await context.SaveChangesAsync(Ct);
        return session.SessionId;
    }

    private async Task<ClassSession> GetSessionAsync(int sessionId)
    {
        await using var context = db.CreateContext();
        return (await context.ClassSessions.FindAsync([sessionId], Ct))!;
    }
}
