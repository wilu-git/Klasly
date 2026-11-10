using System.Data.Common;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Klasly.Tests;

/// <summary>
/// The risk step's inputs query on the real schema. The query reads every enrollment in the database,
/// so each test makes its own class and checks only its own enrollments.
/// </summary>
public class RiskRepositoryTests(DatabaseFixture db)
{
    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Counts_only_this_classes_finalized_sessions_and_leave_out_excused()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var otherClassId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));

        await RecordAsync(classId, SessionStatus.Finalized, 1, AttendanceStatus.Present);
        await RecordAsync(classId, SessionStatus.Finalized, 1, AttendanceStatus.Late);   // attended
        await RecordAsync(classId, SessionStatus.Finalized, 1, AttendanceStatus.Absent);
        await RecordAsync(classId, SessionStatus.Finalized, 1, AttendanceStatus.Excused); // out of both counts
        await RecordAsync(classId, SessionStatus.Finalized, 1, AttendanceStatus.Excused); // two, so Excused ≠ Late in the total
        await RecordAsync(classId, SessionStatus.Closed, 1, AttendanceStatus.Absent);     // not finalized yet
        await RecordAsync(otherClassId, SessionStatus.Finalized, 1, AttendanceStatus.Absent); // another class

        var input = await InputAsync(classId, 1);

        Assert.Equal(1, input.AbsentCount);
        Assert.Equal(3, input.CountedCount);
    }

    [Fact]
    public async Task Sums_use_only_the_students_own_grades_and_skip_blanks()
    {
        var classId = await TestData.NewClassAsync(db,
            (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active), (3, EnrollmentStatus.Active));
        var quiz = await AssessmentAsync(classId, max: 20, weight: 30);
        var exam = await AssessmentAsync(classId, max: 50, weight: 20);
        var project = await AssessmentAsync(classId, max: 10, weight: 50);
        await GradeAsync(quiz, 1, 18);
        await GradeAsync(exam, 1, 30);   // student 1 has no project grade (blank)
        await GradeAsync(project, 2, 7);

        var one = await InputAsync(classId, 1);
        Assert.Equal(3900m, one.WeightedSum); // 18/20 × 100 × 30 + 30/50 × 100 × 20 → average 78.00
        Assert.Equal(50m, one.WeightSum);

        var two = await InputAsync(classId, 2);
        Assert.Equal(3500m, two.WeightedSum);
        Assert.Equal(50m, two.WeightSum);

        var three = await InputAsync(classId, 3);
        Assert.Equal(0m, three.WeightSum);
        Assert.Null(three.Latest);
    }

    [Fact]
    public async Task Dropped_enrollments_and_inactive_terms_are_left_out()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Dropped));
        var pastClassId = await NewClassInInactiveTermAsync(studentId: 1);

        var ids = (await ListAsync()).Select(i => i.EnrollmentId).ToHashSet();

        Assert.Contains(await EnrollmentIdAsync(classId, 1), ids);
        Assert.DoesNotContain(await EnrollmentIdAsync(classId, 2), ids);
        Assert.DoesNotContain(await EnrollmentIdAsync(pastClassId, 1), ids);
    }

    [Fact]
    public async Task Latest_snapshot_is_newest_by_time_then_by_id()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var first = await EnrollmentIdAsync(classId, 1);
        await FlagAsync(first, Now, 0.5m);
        await FlagAsync(first, Now.AddHours(-1), 0.1m); // added later but older: not the latest

        var second = await EnrollmentIdAsync(classId, 2);
        await FlagAsync(second, Now, 0.1m);
        await FlagAsync(second, Now, 0.3m);             // same second: the higher id wins

        Assert.Equal(new RiskSnapshot(0.5m, 78m, true), (await InputAsync(classId, 1)).Latest);
        Assert.Equal(0.3m, (await InputAsync(classId, 2)).Latest!.AbsenceRate);
    }

    [Fact]
    public async Task Inputs_are_loaded_in_one_database_command()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        await GradeAsync(await AssessmentAsync(classId, max: 10, weight: 10), 1, 5);
        var counter = new CommandCounter();

        await using var context = db.CreateContext(counter);
        await new RiskRepository(context).ListInputsAsync(Ct);

        Assert.Equal(1, counter.Count);
    }

    // ---------- helpers ----------

    private async Task<List<RiskInput>> ListAsync()
    {
        await using var context = db.CreateContext();
        return await new RiskRepository(context).ListInputsAsync(Ct);
    }

    private async Task<RiskInput> InputAsync(int classId, int studentId)
    {
        var enrollmentId = await EnrollmentIdAsync(classId, studentId);
        return (await ListAsync()).Single(i => i.EnrollmentId == enrollmentId);
    }

    private async Task<int> EnrollmentIdAsync(int classId, int studentId)
    {
        await using var context = db.CreateContext();
        return await context.Enrollments.Where(e => e.ClassId == classId && e.StudentId == studentId)
            .Select(e => e.EnrollmentId).SingleAsync(Ct);
    }

    private async Task RecordAsync(int classId, SessionStatus sessionStatus, int studentId, AttendanceStatus status)
    {
        await using var context = db.CreateContext();
        var session = new ClassSession
        {
            ClassId = classId,
            StartTime = Now.AddHours(-2),
            EndTime = Now.AddHours(-1),
            QrNonce = Guid.NewGuid().ToString("N"),
            Status = sessionStatus
        };
        session.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentId = studentId, Status = status, Source = AttendanceSource.Manual, MarkedAt = Now
        });
        context.ClassSessions.Add(session);
        await context.SaveChangesAsync(Ct);
    }

    private async Task<int> AssessmentAsync(int classId, decimal max, decimal weight)
    {
        await using var context = db.CreateContext();
        var assessment = new Assessment
        {
            ClassId = classId, Title = $"A-{Guid.NewGuid():N}", Type = "Quiz", MaxScore = max, WeightPercent = weight
        };
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(Ct);
        return assessment.AssessmentId;
    }

    private async Task GradeAsync(int assessmentId, int studentId, decimal score)
    {
        await using var context = db.CreateContext();
        context.Grades.Add(new Grade { AssessmentId = assessmentId, StudentId = studentId, Score = score, PostedAt = Now });
        await context.SaveChangesAsync(Ct);
    }

    private async Task FlagAsync(int enrollmentId, DateTime computedAt, decimal rate)
    {
        await using var context = db.CreateContext();
        context.RiskFlags.Add(new RiskFlag
        {
            EnrollmentId = enrollmentId, AbsenceRate = rate, WeightedAverage = 78m, IsAtRisk = true, ComputedAt = computedAt
        });
        await context.SaveChangesAsync(Ct);
    }

    private async Task<int> NewClassInInactiveTermAsync(int studentId)
    {
        await using var context = db.CreateContext();
        var term = new AcademicTerm
        {
            Name = $"Past-{Guid.NewGuid():N}", StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2025, 10, 31), IsActive = false
        };
        var cls = new Class { SubjectId = 1, TeacherId = 1, Term = term, Section = $"P-{Guid.NewGuid():N}"[..30] };
        cls.Enrollments.Add(new Enrollment { StudentId = studentId, Status = EnrollmentStatus.Active });
        context.Classes.Add(cls);
        await context.SaveChangesAsync(Ct);
        return cls.ClassId;
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
