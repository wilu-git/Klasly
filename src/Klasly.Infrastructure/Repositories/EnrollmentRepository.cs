using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IEnrollmentRepository"/>.</summary>
public class EnrollmentRepository(KlaslyDbContext context) : IEnrollmentRepository
{
    public Task<List<Enrollment>> ListByClassAsync(int classId, CancellationToken ct) =>
        context.Enrollments.AsNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Where(e => e.ClassId == classId)
            .OrderBy(e => e.Student.User.FullName)
            .ToListAsync(ct);

    public Task<Enrollment?> GetByIdAsync(int enrollmentId, CancellationToken ct) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId, ct);

    public Task<Enrollment?> GetByClassAndStudentAsync(int classId, int studentId, CancellationToken ct) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId, ct);

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
