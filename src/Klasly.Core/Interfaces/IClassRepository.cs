using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for classes. Classes are never deleted (no soft-delete column).</summary>
public interface IClassRepository
{
    /// <summary>Classes with Subject, Teacher.User, Term and only the Active enrollments loaded.</summary>
    Task<List<Class>> ListAsync(int? termId, CancellationToken ct);

    /// <summary>One class loaded like <see cref="ListAsync"/>, not tracked — for responses.</summary>
    Task<Class?> GetDetailsAsync(int classId, CancellationToken ct);

    /// <summary>Tracked, no related data — for edits.</summary>
    Task<Class?> GetByIdAsync(int classId, CancellationToken ct);

    Task<bool> ExistsAsync(int subjectId, string section, int termId, int? excludeClassId, CancellationToken ct);
    Task AddAsync(Class cls, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
