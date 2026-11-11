using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IAssessmentRepository"/>.</summary>
public class AssessmentRepository(KlaslyDbContext context) : IAssessmentRepository
{
    public Task<List<Assessment>> ListByClassAsync(int classId, CancellationToken ct) =>
        context.Assessments.AsNoTracking().Where(a => a.ClassId == classId).OrderBy(a => a.AssessmentId).ToListAsync(ct);

    public Task<Assessment?> GetByIdAsync(int assessmentId, CancellationToken ct) =>
        context.Assessments.Include(a => a.Class).ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId, ct);

    public async Task<decimal> SumWeightsAsync(int classId, int? excludeAssessmentId, CancellationToken ct) =>
        await context.Assessments
            .Where(a => a.ClassId == classId && a.AssessmentId != excludeAssessmentId)
            .SumAsync(a => (decimal?)a.WeightPercent, ct) ?? 0;

    public Task<bool> TitleExistsAsync(int classId, string title, int? excludeAssessmentId, CancellationToken ct) =>
        context.Assessments.AnyAsync(a => a.ClassId == classId && a.Title == title && a.AssessmentId != excludeAssessmentId, ct);

    public Task<decimal?> MaxPostedScoreAsync(int assessmentId, CancellationToken ct) =>
        context.Grades.Where(g => g.AssessmentId == assessmentId).MaxAsync(g => (decimal?)g.Score, ct);

    public Task<Dictionary<int, int>> CountGradedAsync(int classId, CancellationToken ct) =>
        context.Grades
            .Where(g => g.Assessment.ClassId == classId && context.Enrollments.Any(e =>
                e.ClassId == classId && e.StudentId == g.StudentId && e.Status == EnrollmentStatus.Active))
            .GroupBy(g => g.AssessmentId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), ct);

    public async Task AddAsync(Assessment assessment, CancellationToken ct)
    {
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
