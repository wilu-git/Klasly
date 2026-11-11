using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for the subject catalog. Subjects are never deleted (no soft-delete column).</summary>
public interface ISubjectRepository
{
    Task<List<Subject>> ListAsync(CancellationToken ct);
    Task<Subject?> GetByIdAsync(int subjectId, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, int? excludeSubjectId, CancellationToken ct);
    Task AddAsync(Subject subject, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
