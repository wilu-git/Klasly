using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// The student overview read by the student app and the parent portal (Milestone 13).
/// Student 1 is in many test classes, so each test makes its own class and checks only that class.
/// In the "Risk" collection so <see cref="RiskCalculatorTests"/> never adds a newer snapshot mid-test.
/// </summary>
[Collection("Risk")]
public class OverviewTests(DatabaseFixture db)
{
    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Lists_only_active_enrollments_in_the_active_term()
    {
        var activeId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var droppedId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Dropped));
        var pastId = await NewClassInInactiveTermAsync(studentId: 1);

        var overview = await GetAsync(1);

        var ids = overview.Classes.Select(c => c.ClassId).ToHashSet();
        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(droppedId, ids);
        Assert.DoesNotContain(pastId, ids);
        Assert.Equal(1, overview.StudentId);
        Assert.False(string.IsNullOrWhiteSpace(overview.FullName));
        Assert.False(string.IsNullOrWhiteSpace(overview.StudentNumber));
        Assert.False(string.IsNullOrWhiteSpace(overview.TermName));
    }

    [Fact]
    public async Task Class_shows_subject_section_and_teacher()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        await using var context = db.CreateContext();
        var expected = await context.Classes.Where(c => c.ClassId == classId)
            .Select(c => new { c.Subject.Code, c.Subject.Title, c.Section, Teacher = c.Teacher.User.FullName })
            .SingleAsync(Ct);

        var cls = ClassOf(await GetAsync(1), classId);

        Assert.Equal(expected.Code, cls.SubjectCode);
        Assert.Equal(expected.Title, cls.SubjectTitle);
        Assert.Equal(expected.Section, cls.Section);
        Assert.Equal(expected.Teacher, cls.TeacherName);
    }

    [Fact]
    public async Task Counts_and_records_are_this_students_newest_first()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var otherClassId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        await RecordAsync(classId, Now.AddDays(-3), 1, AttendanceStatus.Present);
        await RecordAsync(classId, Now.AddDays(-1), 1, AttendanceStatus.Absent);
        await RecordAsync(classId, Now.AddDays(-2), 1, AttendanceStatus.Late);
        await RecordAsync(classId, Now.AddDays(-4), 1, AttendanceStatus.Excused);
        await RecordAsync(classId, Now.AddDays(-1), 2, AttendanceStatus.Absent);      // another student
        await RecordAsync(otherClassId, Now.AddDays(-1), 1, AttendanceStatus.Absent); // another class

        var cls = ClassOf(await GetAsync(1), classId);

        Assert.Equal((1, 1, 1, 1), (cls.Present, cls.Late, cls.Absent, cls.Excused));
        Assert.Equal(["Absent", "Late", "Present", "Excused"], cls.Attendance.Select(a => a.Status));
        Assert.Equal(Now.AddDays(-1), cls.Attendance[0].StartTimeUtc);
    }

    [Fact]
    public async Task Every_assessment_is_listed_with_only_this_students_score()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        var quiz = await AssessmentAsync(classId, "Quiz 1", Now.AddDays(-5), max: 20, weight: 40);
        var exam = await AssessmentAsync(classId, "Exam", Now.AddDays(-1), max: 50, weight: 60);
        await GradeAsync(quiz, 1, 18.5m);
        await GradeAsync(exam, 2, 40m); // student 1 has no exam grade yet

        var grades = ClassOf(await GetAsync(1), classId).Grades;

        Assert.Equal(["Quiz 1", "Exam"], grades.Select(g => g.Title));
        Assert.Equal(new GradeItem(quiz, "Quiz 1", "Quiz", 20m, 40m, 18.5m, Now), grades[0]);
        Assert.Null(grades[1].Score);
        Assert.Null(grades[1].PostedAtUtc);
    }

    [Fact]
    public async Task Risk_is_the_latest_snapshot_or_null()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var noFlagsId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var enrollmentId = await EnrollmentIdAsync(classId, 1);
        await FlagAsync(enrollmentId, Now, 0.25m, 80m, true);
        await FlagAsync(enrollmentId, Now.AddHours(-1), 0.10m, 90m, false); // added later but older
        await FlagAsync(enrollmentId, Now, 0.30m, null, true);              // same second, higher id: the latest

        var overview = await GetAsync(1);

        var cls = ClassOf(overview, classId);
        Assert.Equal((0.30m, (decimal?)null, (bool?)true, (DateTime?)Now),
            (cls.AbsenceRate!.Value, cls.WeightedAverage, cls.IsAtRisk, cls.RiskComputedAtUtc));

        var none = ClassOf(overview, noFlagsId);
        Assert.Null(none.AbsenceRate);
        Assert.Null(none.WeightedAverage);
        Assert.Null(none.IsAtRisk);
        Assert.Null(none.RiskComputedAtUtc);
    }

    [Fact]
    public async Task Unknown_student_is_null()
    {
        await using var context = db.CreateContext();
        Assert.Null(await new OverviewRepository(context).GetAsync(999_999, Ct));
    }

    // ---------- helpers ----------

    private async Task<StudentOverviewResponse> GetAsync(int studentId)
    {
        await using var context = db.CreateContext();
        return (await new OverviewRepository(context).GetAsync(studentId, Ct))!;
    }

    private static ClassOverview ClassOf(StudentOverviewResponse overview, int classId) =>
        overview.Classes.Single(c => c.ClassId == classId);

    private async Task<int> EnrollmentIdAsync(int classId, int studentId)
    {
        await using var context = db.CreateContext();
        return await context.Enrollments.Where(e => e.ClassId == classId && e.StudentId == studentId)
            .Select(e => e.EnrollmentId).SingleAsync(Ct);
    }

    private async Task RecordAsync(int classId, DateTime start, int studentId, AttendanceStatus status)
    {
        await using var context = db.CreateContext();
        var session = new ClassSession
        {
            ClassId = classId,
            StartTime = start,
            EndTime = start.AddHours(1),
            QrNonce = Guid.NewGuid().ToString("N"),
            Status = SessionStatus.Finalized
        };
        session.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentId = studentId, Status = status, Source = AttendanceSource.Manual, MarkedAt = Now
        });
        context.ClassSessions.Add(session);
        await context.SaveChangesAsync(Ct);
    }

    private async Task<int> AssessmentAsync(int classId, string title, DateTime posted, decimal max, decimal weight)
    {
        await using var context = db.CreateContext();
        var assessment = new Assessment
        {
            ClassId = classId, Title = title, Type = "Quiz", MaxScore = max, WeightPercent = weight, DatePosted = posted
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

    private async Task FlagAsync(int enrollmentId, DateTime computedAt, decimal rate, decimal? average, bool atRisk)
    {
        await using var context = db.CreateContext();
        context.RiskFlags.Add(new RiskFlag
        {
            EnrollmentId = enrollmentId, AbsenceRate = rate, WeightedAverage = average, IsAtRisk = atRisk, ComputedAt = computedAt
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
}
