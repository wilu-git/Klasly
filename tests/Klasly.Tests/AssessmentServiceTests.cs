using Klasly.API.Push;
using Klasly.API.Services;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;

namespace Klasly.Tests;

/// <summary>Assessment rules (guide §27.7/§27.8): weights per class never above 100%, max score ≥ any posted score, own classes only.</summary>
public class AssessmentServiceTests(DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Class_weights_may_total_exactly_100_but_not_more()
    {
        var classId = await TestData.NewClassAsync(db);
        await CreateAsync(classId, "Quiz 1", 20, 30);
        await CreateAsync(classId, "Midterm", 100, 70);

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() => CreateAsync(classId, "Extra", 10, 0.01m));

        Assert.Contains("weightPercent", ex.Errors.Keys);
    }

    [Fact]
    public async Task Editing_counts_the_other_assessments_only()
    {
        var classId = await TestData.NewClassAsync(db);
        var quiz = await CreateAsync(classId, "Quiz 1", 20, 40);
        await CreateAsync(classId, "Midterm", 100, 60);

        var same = await UpdateAsync(quiz.AssessmentId, "Quiz 1", 25, 40); // its own 40% isn't counted twice
        Assert.Equal(25, same.MaxScore);

        await Assert.ThrowsAsync<FieldValidationException>(() => UpdateAsync(quiz.AssessmentId, "Quiz 1", 25, 40.01m));
    }

    [Fact]
    public async Task Values_with_more_than_two_decimals_are_refused()
    {
        var classId = await TestData.NewClassAsync(db);

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() => CreateAsync(classId, "Quiz 1", 20.005m, 10));

        Assert.Contains("maxScore", ex.Errors.Keys);
    }

    [Fact]
    public async Task Max_score_cannot_go_below_a_posted_score()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        var quiz = await CreateAsync(classId, "Quiz 1", 20, 30);
        await using (var context = db.CreateContext())
        {
            context.Grades.Add(new Grade { AssessmentId = quiz.AssessmentId, StudentId = 1, Score = 18 });
            await context.SaveChangesAsync(Ct);
        }

        var ex = await Assert.ThrowsAsync<FieldValidationException>(() => UpdateAsync(quiz.AssessmentId, "Quiz 1", 17.99m, 30));
        Assert.Contains("18", ex.Errors["maxScore"][0]);

        Assert.Equal(18, (await UpdateAsync(quiz.AssessmentId, "Quiz 1", 18, 30)).MaxScore);
    }

    [Fact]
    public async Task Duplicate_title_in_the_same_class_is_a_conflict()
    {
        var classId = await TestData.NewClassAsync(db);
        await CreateAsync(classId, "Quiz 1", 20, 10);

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(classId, " Quiz 1 ", 20, 10));
    }

    [Fact]
    public async Task Another_teachers_class_and_assessment_are_forbidden()
    {
        var classId = await TestData.NewClassAsync(db);
        var quiz = await CreateAsync(classId, "Quiz 1", 20, 10);

        await using var context = db.CreateContext();
        var service = NewService(context);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(classId, Request("Hack", 10, 10), TestData.MarkUserId, Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(quiz.AssessmentId, TestData.MarkUserId, Ct));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAsync(int.MaxValue, TestData.RosaUserId, Ct));
    }

    [Fact]
    public async Task List_shows_graded_count_of_active_students_and_the_class_size()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active), (3, EnrollmentStatus.Dropped));
        var quiz = await CreateAsync(classId, "Quiz 1", 20, 30);
        await using (var context = db.CreateContext())
        {
            context.Grades.AddRange(
                new Grade { AssessmentId = quiz.AssessmentId, StudentId = 1, Score = 15 },
                new Grade { AssessmentId = quiz.AssessmentId, StudentId = 3, Score = 10 }); // dropped: not counted
            await context.SaveChangesAsync(Ct);
        }

        await using var read = db.CreateContext();
        var row = Assert.Single(await NewService(read).ListAsync(classId, TestData.RosaUserId, Ct));

        Assert.Equal((1, 2), (row.GradedCount, row.ActiveCount));
    }

    // ---------- helpers ----------

    private static AssessmentService NewService(KlaslyDbContext context) => new(
        new AssessmentRepository(context), new ClassRepository(context), new UserRepository(context),
        new EnrollmentRepository(context), new GradeRepository(context), new NotificationRepository(context), new PushQueue());

    private static AssessmentRequest Request(string title, decimal max, decimal weight) =>
        new() { Title = title, Type = "Quiz", MaxScore = max, WeightPercent = weight };

    private async Task<AssessmentResponse> CreateAsync(int classId, string title, decimal max, decimal weight)
    {
        await using var context = db.CreateContext();
        return await NewService(context).CreateAsync(classId, Request(title, max, weight), TestData.RosaUserId, Ct);
    }

    private async Task<AssessmentResponse> UpdateAsync(int assessmentId, string title, decimal max, decimal weight)
    {
        await using var context = db.CreateContext();
        return await NewService(context).UpdateAsync(assessmentId, Request(title, max, weight), TestData.RosaUserId, Ct);
    }
}
