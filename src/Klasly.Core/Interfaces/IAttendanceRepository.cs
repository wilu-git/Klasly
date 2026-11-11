using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for attendance records. (SessionId, StudentId) is unique.</summary>
public interface IAttendanceRepository
{
    Task<bool> ExistsAsync(int sessionId, int studentId, CancellationToken ct);

    /// <summary>Tracked; null when the student has no record for the session yet.</summary>
    Task<AttendanceRecord?> GetAsync(int sessionId, int studentId, CancellationToken ct);

    /// <summary>All records of a session, not tracked.</summary>
    Task<List<AttendanceRecord>> ListBySessionAsync(int sessionId, CancellationToken ct);

    /// <summary>Inserts the record; false when a record for the same session and student already exists (lost race).</summary>
    Task<bool> TryAddAsync(AttendanceRecord record, CancellationToken ct);

    /// <summary>Tracks the record; it is written by the next <see cref="TrySaveAsync"/>.</summary>
    void Add(AttendanceRecord record);

    /// <summary>
    /// Saves every pending change (records and session status) in one transaction. False, with nothing saved and the
    /// change tracker cleared, when another request inserted a record for the same session and student first.
    /// </summary>
    Task<bool> TrySaveAsync(CancellationToken ct);
}
