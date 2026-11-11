using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for attendance records. (SessionId, StudentId) is unique.</summary>
public interface IAttendanceRepository
{
    Task<bool> ExistsAsync(int sessionId, int studentId, CancellationToken ct);

    /// <summary>All records of a session, not tracked.</summary>
    Task<List<AttendanceRecord>> ListBySessionAsync(int sessionId, CancellationToken ct);

    /// <summary>Inserts the record; false when a record for the same session and student already exists (lost race).</summary>
    Task<bool> TryAddAsync(AttendanceRecord record, CancellationToken ct);
}
