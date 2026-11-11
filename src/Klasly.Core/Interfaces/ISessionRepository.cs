using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for class sessions. Sessions are never deleted.</summary>
public interface ISessionRepository
{
    /// <summary>Tracked, with Class and Class.Subject loaded.</summary>
    Task<ClassSession?> GetByIdAsync(int sessionId, CancellationToken ct);

    /// <summary>Sessions of a class with Class.Subject loaded, newest start first.</summary>
    Task<List<ClassSession>> ListByClassAsync(int classId, CancellationToken ct);

    Task AddAsync(ClassSession session, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
