using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IRiskRepository"/>.</summary>
public class RiskRepository(KlaslyDbContext context) : IRiskRepository
{
    public async Task<List<RiskInput>> ListInputsAsync(CancellationToken ct)
    {
        var rows = await context.Enrollments.AsNoTracking()
            .Where(e => e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive)
            .Select(e => new
            {
                e.EnrollmentId,
                Absent = context.AttendanceRecords.Count(a =>
                    a.StudentId == e.StudentId && a.Session.ClassId == e.ClassId
                    && a.Session.Status == SessionStatus.Finalized && a.Status == AttendanceStatus.Absent),
                Counted = context.AttendanceRecords.Count(a =>
                    a.StudentId == e.StudentId && a.Session.ClassId == e.ClassId
                    && a.Session.Status == SessionStatus.Finalized && a.Status != AttendanceStatus.Excused),
                Grades = context.Grades
                    .Where(g => g.StudentId == e.StudentId && g.Assessment.ClassId == e.ClassId)
                    .Select(g => new { g.Score, g.Assessment.MaxScore, g.Assessment.WeightPercent })
                    .ToList(),
                Latest = e.RiskFlags
                    .OrderByDescending(r => r.ComputedAt).ThenByDescending(r => r.RiskFlagId)
                    .Select(r => new RiskSnapshot(r.AbsenceRate, r.WeightedAverage, r.IsAtRisk))
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        // The sums are done here in C# decimals, not in SQL: SQL Server's decimal division can drop digits
        // and move a value across the 75.00 boundary. Multiplying before dividing keeps 18/20 × 100 × 30 exact.
        return rows.Select(r => new RiskInput(
                r.EnrollmentId,
                r.Absent,
                r.Counted,
                r.Grades.Sum(g => g.Score * 100 * g.WeightPercent / g.MaxScore),
                r.Grades.Sum(g => g.WeightPercent),
                r.Latest))
            .ToList();
    }

    public void Add(RiskFlag flag) => context.RiskFlags.Add(flag);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
