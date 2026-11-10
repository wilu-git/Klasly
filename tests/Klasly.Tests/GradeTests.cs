using Klasly.API.Push;
using Klasly.API.Services;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// Grade entry rules (guide §27.8, spec assumptions 3–4): 0 ≤ score ≤ MaxScore with at most 2 decimals, active students only,
/// all-or-nothing saves, and one Pending push row for a student's first grade on an assessment.
/// </summary>
public class GradeTests(DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Zero_and_exactly_the_max_score_are_saved()
    {
        var (_, quiz) = await NewQuizAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));

        var result = await SaveAsync(quiz, (1, 0m), (2, 20m));

        Assert.Equal((2, 0), (result.Added, result.Changed));
        Assert.Equal([0m, 20m], result.Grades.Select(g => g.Score!.Value).Order());
    }

    [Fact]
    public async Task One_bad_row_saves_nothing_and_each_bad_row_gets_its_own_error()
    {
        var (_, quiz) = await NewQuizAsync(
            (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active), (3, EnrollmentStatus.Active), (4, EnrollmentStatus.Active));

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() =>
            SaveAsync(quiz, (1, 10m), (2, -0.01m), (3, 20.01m), (4, 10.005m)));

        Assert.Equal(["grades[1].score", "grades[2].score", "grades[3].score"], ex.Errors.Keys.Order());
        Assert.Equal("Score must be between 0 and 20.", ex.Errors["grades[2].score"][0]);
        Assert.Empty(await GradesInDbAsync(quiz));
    }

    [Fact]
    public async Task A_student_who_is_not_actively_enrolled_is_refused()
    {
        var (_, quiz) = await NewQuizAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Dropped));

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() => SaveAsync(quiz, (2, 10m)));

        Assert.Contains("grades[0].studentId", ex.Errors.Keys);
    }

    [Fact]
    public async Task Blank_scores_are_skipped()
    {
        var (_, quiz) = await NewQuizAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));

        var result = await SaveAsync(quiz, (1, 12m), (2, null));

        Assert.Equal(1, result.Added);
        Assert.Null(result.Grades.Single(g => g.StudentId == 2).Score);
    }

    [Fact]
    public async Task First_grade_queues_one_push_a_changed_score_none_and_the_same_score_nothing()
    {
        var (_, quiz) = await NewQuizAsync((1, EnrollmentStatus.Active));

        await SaveAsync(quiz, (1, 15m));
        var changed = await SaveAsync(quiz, (1, 16.5m));
        var same = await SaveAsync(quiz, (1, 16.50m));

        Assert.Equal((0, 1), (changed.Added, changed.Changed));
        Assert.Equal((0, 0), (same.Added, same.Changed));

        await using var context = db.CreateContext();
        var push = Assert.Single(await context.Notifications.AsNoTracking()
            .Where(n => n.DedupKey == $"grade:{quiz}:1").ToListAsync(Ct));
        Assert.Equal(
            (4, NotificationChannel.Push, NotificationType.GradePosted, NotificationStatus.Pending, "New grade posted"),
            (push.UserId, push.Channel, push.Type, push.Status, push.Subject));
        Assert.Equal("ELECT3 – Quiz 1: 15 / 20", push.Body);

        var grade = Assert.Single(await GradesInDbAsync(quiz));
        Assert.Equal(16.5m, grade.Score);
        Assert.NotNull(grade.UpdatedAt);
    }

    [Fact]
    public async Task Date_posted_is_set_by_the_first_grade_only()
    {
        var (_, quiz) = await NewQuizAsync((1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        await SaveAsync(quiz, (1, 10m));
        var first = await DatePostedAsync(quiz);

        await Task.Delay(1100, Ct); // datetime2(0): make a second save land on a later second
        await SaveAsync(quiz, (2, 11m));

        Assert.NotNull(first);
        Assert.Equal(first, await DatePostedAsync(quiz));
    }

    // ---------- helpers ----------

    private static AssessmentService NewService(KlaslyDbContext context) => new(
        new AssessmentRepository(context), new ClassRepository(context), new UserRepository(context),
        new EnrollmentRepository(context), new GradeRepository(context), new NotificationRepository(context), new PushQueue());

    /// <summary>A new class with the seats and a "Quiz 1" worth 20 points; returns (classId, assessmentId).</summary>
    private async Task<(int ClassId, int AssessmentId)> NewQuizAsync(params (int StudentId, EnrollmentStatus Status)[] seats)
    {
        var classId = await TestData.NewClassAsync(db, seats);
        await using var context = db.CreateContext();
        var quiz = await NewService(context).CreateAsync(
            classId, new AssessmentRequest { Title = "Quiz 1", Type = "Quiz", MaxScore = 20, WeightPercent = 30 }, TestData.RosaUserId, Ct);
        return (classId, quiz.AssessmentId);
    }

    private async Task<SaveGradesResponse> SaveAsync(int assessmentId, params (int StudentId, decimal? Score)[] rows)
    {
        await using var context = db.CreateContext();
        var request = new SaveGradesRequest { Grades = rows.Select(r => new GradeEntry { StudentId = r.StudentId, Score = r.Score }).ToList() };
        return await NewService(context).SaveGradesAsync(assessmentId, request, TestData.RosaUserId, Ct);
    }

    private async Task<List<Grade>> GradesInDbAsync(int assessmentId)
    {
        await using var context = db.CreateContext();
        return await context.Grades.AsNoTracking().Where(g => g.AssessmentId == assessmentId).ToListAsync(Ct);
    }

    private async Task<DateTime?> DatePostedAsync(int assessmentId)
    {
        await using var context = db.CreateContext();
        return (await context.Assessments.FindAsync([assessmentId], Ct))!.DatePosted;
    }
}
