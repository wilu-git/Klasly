using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ITermRepository"/>.</summary>
public class TermRepository(KlaslyDbContext context) : ITermRepository
{
    public Task<List<AcademicTerm>> ListAsync(CancellationToken ct) =>
        context.AcademicTerms.AsNoTracking().OrderByDescending(t => t.StartDate).ToListAsync(ct);

    // Tracked, so the service can change properties and call SaveChangesAsync.
    public Task<AcademicTerm?> GetByIdAsync(int termId, CancellationToken ct) =>
        context.AcademicTerms.FirstOrDefaultAsync(t => t.TermId == termId, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeTermId, CancellationToken ct) =>
        context.AcademicTerms.AnyAsync(t => t.Name == name && t.TermId != excludeTermId, ct);

    public async Task AddAsync(AcademicTerm term, CancellationToken ct)
    {
        context.AcademicTerms.Add(term);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    public async Task ActivateAsync(AcademicTerm term, CancellationToken ct)
    {
        // Clear the others first: UX_AcademicTerms_OneActive allows only one IsActive = 1 row at any moment.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.AcademicTerms
            .Where(t => t.IsActive && t.TermId != term.TermId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false), ct);
        term.IsActive = true;
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
