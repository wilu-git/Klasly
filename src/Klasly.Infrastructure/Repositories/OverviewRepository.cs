using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOverviewRepository"/>.</summary>
public class OverviewRepository(KlaslyDbContext context) : IOverviewRepository
{
    public async Task<StudentOverviewResponse?> GetAsync(int studentId, CancellationToken ct)
    {
        var student = await context.Students.AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .Select(s => new { s.StudentNumber, s.User.FullName })
            .SingleOrDefaultAsync(ct);
        if (student is null) return null;

        var termName = await context.AcademicTerms.AsNoTracking()
            .Where(t => t.IsActive).Select(t => t.Name).SingleOrDefaultAsync(ct);

        var rows = await context.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive)
            .OrderBy(e => e.Class.Subject.Code).ThenBy(e => e.Class.Section)
            .Select(e => new
            {
                e.ClassId,
                e.Class.Subject.Code,
                e.Class.Subject.Title,
                e.Class.Section,
                Teacher = e.Class.Teacher.User.FullName,
                Records = context.AttendanceRecords
                    .Where(a => a.StudentId == studentId && a.Session.ClassId == e.ClassId)
                    .OrderByDescending(a => a.Session.StartTime).ThenByDescending(a => a.SessionId)
                    .Select(a => new { a.SessionId, a.Session.StartTime, a.Status })
                    .ToList(),
                Grades = e.Class.Assessments
                    .OrderBy(a => a.DatePosted).ThenBy(a => a.Title)
                    .Select(a => new
                    {
                        a.AssessmentId, a.Title, a.Type, a.MaxScore, a.WeightPercent,
                        Mine = a.Grades.Where(g => g.StudentId == studentId)
                            .Select(g => new { g.Score, g.PostedAt }).FirstOrDefault()
                    })
                    .ToList(),
                Latest = e.RiskFlags
                    .OrderByDescending(r => r.ComputedAt).ThenByDescending(r => r.RiskFlagId)
                    .Select(r => new { r.AbsenceRate, r.WeightedAverage, r.IsAtRisk, r.ComputedAt })
                    .FirstOrDefault()
            })
            .AsSplitQuery() // records × assessments in one join would multiply rows
            .ToListAsync(ct);

        var classes = rows.Select(r => new ClassOverview(
                r.ClassId, r.Code, r.Title, r.Section, r.Teacher,
                r.Records.Count(a => a.Status == AttendanceStatus.Present),
                r.Records.Count(a => a.Status == AttendanceStatus.Late),
                r.Records.Count(a => a.Status == AttendanceStatus.Absent),
                r.Records.Count(a => a.Status == AttendanceStatus.Excused),
                r.Latest?.AbsenceRate, r.Latest?.WeightedAverage, r.Latest?.IsAtRisk, r.Latest?.ComputedAt,
                r.Records.Select(a => new AttendanceItem(a.SessionId, a.StartTime, a.Status.ToString())).ToList(),
                r.Grades.Select(g => new GradeItem(
                    g.AssessmentId, g.Title, g.Type, g.MaxScore, g.WeightPercent, g.Mine?.Score, g.Mine?.PostedAt)).ToList()))
            .ToList();

        return new StudentOverviewResponse(studentId, student.StudentNumber, student.FullName, termName, classes);
    }
}
