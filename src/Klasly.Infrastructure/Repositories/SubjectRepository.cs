using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ISubjectRepository"/>.</summary>
public class SubjectRepository(KlaslyDbContext context) : ISubjectRepository
{
    public Task<List<Subject>> ListAsync(CancellationToken ct) =>
        context.Subjects.AsNoTracking().OrderBy(s => s.Code).ToListAsync(ct);

    // Tracked, so the service can change properties and call SaveChangesAsync.
    public Task<Subject?> GetByIdAsync(int subjectId, CancellationToken ct) =>
        context.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId, ct);

    public Task<bool> CodeExistsAsync(string code, int? excludeSubjectId, CancellationToken ct) =>
        context.Subjects.AnyAsync(s => s.Code == code && s.SubjectId != excludeSubjectId, ct);

    public async Task AddAsync(Subject subject, CancellationToken ct)
    {
        context.Subjects.Add(subject);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
