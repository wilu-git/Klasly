using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for class sessions. Sessions are never deleted.</summary>
public interface ISessionRepository
{
    /// <summary>Tracked, with Class and Class.Subject loaded.</summary>
    Task<ClassSession?> GetByIdAsync(int sessionId, CancellationToken ct);

    /// <summary>Sessions of a class with Class.Subject loaded, newest start first.</summary>
    Task<List<ClassSession>> ListByClassAsync(int classId, CancellationToken ct);

    /// <summary>Tracked. Open or Scheduled sessions whose EndTime is at or before <paramref name="nowUtc"/> (for the Worker).</summary>
    Task<List<ClassSession>> ListEndedAsync(DateTime nowUtc, CancellationToken ct);

    /// <summary>Tracked. Every Closed session, waiting for the Worker to mark Absent and finalize.</summary>
    Task<List<ClassSession>> ListClosedAsync(CancellationToken ct);

    Task AddAsync(ClassSession session, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
