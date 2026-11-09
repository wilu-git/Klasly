using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IClassRepository"/>.</summary>
public class ClassRepository(KlaslyDbContext context) : IClassRepository
{
    public Task<List<Class>> ListAsync(int? termId, CancellationToken ct) =>
        WithDetails()
            .Where(c => termId == null || c.TermId == termId)
            .OrderBy(c => c.Subject.Code).ThenBy(c => c.Section)
            .ToListAsync(ct);

    public Task<Class?> GetDetailsAsync(int classId, CancellationToken ct) =>
        WithDetails().FirstOrDefaultAsync(c => c.ClassId == classId, ct);

    public Task<Class?> GetByIdAsync(int classId, CancellationToken ct) =>
        context.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct);

    public Task<bool> ExistsAsync(int subjectId, string section, int termId, int? excludeClassId, CancellationToken ct) =>
        context.Classes.AnyAsync(c =>
            c.SubjectId == subjectId && c.Section == section && c.TermId == termId && c.ClassId != excludeClassId, ct);

    public async Task AddAsync(Class cls, CancellationToken ct)
    {
        context.Classes.Add(cls);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    private IQueryable<Class> WithDetails() =>
        context.Classes.AsNoTracking()
            .Include(c => c.Subject)
            .Include(c => c.Teacher).ThenInclude(t => t.User)
            .Include(c => c.Term)
            .Include(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Active));
}
