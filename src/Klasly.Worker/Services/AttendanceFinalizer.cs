using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.Worker.Services;

/// <summary>
/// The Worker's attendance steps (guide Section 27.8). The job passes "now" in, so every step of one tick uses the same time.
/// </summary>
public class AttendanceFinalizer(
    ISessionRepository sessions,
    IAttendanceRepository attendance,
    IEnrollmentRepository enrollments,
    ILogger<AttendanceFinalizer> logger)
{
    /// <summary>Open sessions past their end become Closed; Scheduled ones that were never opened become Cancelled.</summary>
    public async Task<(int Closed, int Cancelled)> CloseEndedSessionsAsync(DateTime nowUtc, CancellationToken ct)
    {
        var ended = await sessions.ListEndedAsync(nowUtc, ct);
        foreach (var session in ended)
        {
            if (session.Status == SessionStatus.Open)
            {
                session.Status = SessionStatus.Closed;
                session.ClosedAt = nowUtc;
            }
            else
            {
                session.Status = SessionStatus.Cancelled; // never opened, so there is no attendance to take
            }
        }

        await sessions.SaveChangesAsync(ct);
        var closed = ended.Count(s => s.Status == SessionStatus.Closed);
        return (closed, ended.Count - closed);
    }

    /// <summary>
    /// For each Closed session, every actively enrolled student without a record gets Absent (Source Auto), and the
    /// session becomes Finalized, in one save per session. Scans and teacher corrections are never overwritten.
    /// Returns how many sessions were finalized.
    /// </summary>
    public async Task<int> MarkAbsentAndFinalizeAsync(DateTime nowUtc, CancellationToken ct)
    {
        var finalized = 0;
        foreach (var session in await sessions.ListClosedAsync(ct))
        {
            var marked = (await attendance.ListBySessionAsync(session.SessionId, ct)).Select(a => a.StudentId).ToHashSet();
            var absentees = (await enrollments.ListByClassAsync(session.ClassId, ct))
                .Where(e => e.Status == EnrollmentStatus.Active && !marked.Contains(e.StudentId));

            foreach (var enrollment in absentees)
            {
                attendance.Add(new AttendanceRecord
                {
                    SessionId = session.SessionId,
                    StudentId = enrollment.StudentId,
                    Status = AttendanceStatus.Absent,
                    Source = AttendanceSource.Auto,
                    MarkedAt = nowUtc
                });
            }
            session.Status = SessionStatus.Finalized;

            if (!await attendance.TrySaveAsync(ct))
            {
                // A teacher's correction landed after the roster was read. The next tick re-reads it and finishes.
                logger.LogWarning("Session {SessionId} got a new attendance record while finalizing; retrying next tick", session.SessionId);
                break;
            }
            finalized++;
        }
        return finalized;
    }
}
