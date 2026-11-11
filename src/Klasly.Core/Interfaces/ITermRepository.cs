using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for academic terms. "Deleting" a term sets IsActive = false; rows are never removed.</summary>
public interface ITermRepository
{
    Task<List<AcademicTerm>> ListAsync(CancellationToken ct);
    Task<AcademicTerm?> GetByIdAsync(int termId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeTermId, CancellationToken ct);
    Task AddAsync(AcademicTerm term, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Makes <paramref name="term"/> the only active term, in one transaction.</summary>
    Task ActivateAsync(AcademicTerm term, CancellationToken ct);
}
