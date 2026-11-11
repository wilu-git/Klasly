using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Repositories;
using Klasly.Worker.Services;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// The Worker's risk step on the real schema. It writes snapshots for every enrollment in the database, so it shares
/// the "Risk" collection with <see cref="RiskRepositoryTests"/> (never in parallel) and checks only its own class's rows.
/// </summary>
[Collection("Risk")]
public class RiskCalculatorTests(DatabaseFixture db)
{
    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_run_writes_one_snapshot_per_enrollment_from_the_rules()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        await AbsentInFinalizedSessionAsync(classId, 1);              // 1 of 1 → 100% absent
        await GradeAsync(classId, 2, score: 70, max: 100, weight: 40); // 70.00 average

        await RunAsync(Now);

        var one = Assert.Single(await FlagsAsync(classId, 1));
        Assert.Equal((1.0000m, (decimal?)null, true, Now), (one.AbsenceRate, one.WeightedAverage, one.IsAtRisk, one.ComputedAt));
        var two = Assert.Single(await FlagsAsync(classId, 2));
        Assert.Equal((0m, (decimal?)70.00m, true), (two.AbsenceRate, two.WeightedAverage, two.IsAtRisk));
    }

    [Fact]
    public async Task Unchanged_values_write_nothing_and_a_change_writes_one_new_row()
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        await RunAsync(Now);
        await RunAsync(Now.AddMinutes(5));

        Assert.Single(await FlagsAsync(classId, 1));
        Assert.Single(await FlagsAsync(classId, 2));

        await AbsentInFinalizedSessionAsync(classId, 1);
        await GradeAsync(classId, 2, score: 90, max: 100, weight: 50);
        await RunAsync(Now.AddMinutes(10));

        var one = await FlagsAsync(classId, 1);
        Assert.Equal(2, one.Count);
        Assert.Equal((0m, false), (one[0].AbsenceRate, one[0].IsAtRisk)); // the old row is history, unchanged
        Assert.Equal((1m, true, Now.AddMinutes(10)), (one[1].AbsenceRate, one[1].IsAtRisk, one[1].ComputedAt));

        var two = await FlagsAsync(classId, 2);
        Assert.Equal(2, two.Count);
        Assert.Equal(90.00m, two[1].WeightedAverage);
        Assert.False(two[1].IsAtRisk);
    }

    [Fact]
    public async Task Exactly_20_percent_absent_is_at_risk_and_exactly_75_is_not_with_the_seeded_settings()
    {
        // Guide §27.8 boundaries, through the real step and the SystemSettings rows (0.20 / 75), not constants.
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active), (2, EnrollmentStatus.Active));
        await AbsentInFinalizedSessionAsync(classId, 1);
        for (var i = 0; i < 4; i++) await AbsentInFinalizedSessionAsync(classId, 1, AttendanceStatus.Present); // 1 of 5
        await GradeAsync(classId, 2, score: 75, max: 100, weight: 100);                                       // 75.00

        await RunAsync(Now);

        var one = Assert.Single(await FlagsAsync(classId, 1));
        Assert.Equal((0.2000m, true), (one.AbsenceRate, one.IsAtRisk));
        var two = Assert.Single(await FlagsAsync(classId, 2));
        Assert.Equal((0m, (decimal?)75.00m, false), (two.AbsenceRate, two.WeightedAverage, two.IsAtRisk));
    }

    [Theory]
    [InlineData(null, "75", "AbsenceThreshold")]
    [InlineData("0.20", "seventy-five", "PassingGrade")]
    [InlineData("20", "75", "AbsenceThreshold")]  // typed as a percent: would silently flag nobody
    [InlineData("0.20", "0.75", "PassingGrade")]  // typed as a fraction: would silently flag nobody
    [InlineData("0", "75", "AbsenceThreshold")]   // 0 would flag everyone
    [InlineData("0.20", "101", "PassingGrade")]
    public async Task Missing_or_bad_setting_fails_the_step_and_saves_nothing(string? threshold, string? passing, string key)
    {
        var classId = await TestData.NewClassAsync(db, (1, EnrollmentStatus.Active));
        await using var context = db.CreateContext();
        var calculator = new RiskCalculator(new RiskRepository(context), new StubSettings(threshold, passing));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => calculator.RecomputeAsync(Now, Ct));

        Assert.Contains(key, error.Message);
        Assert.Empty(await FlagsAsync(classId, 1));
    }

    // ---------- helpers ----------

    private async Task RunAsync(DateTime now)
    {
        await using var context = db.CreateContext();
        await new RiskCalculator(new RiskRepository(context), new SystemSettingRepository(context)).RecomputeAsync(now, Ct);
    }

    private async Task<List<RiskFlag>> FlagsAsync(int classId, int studentId)
    {
        await using var context = db.CreateContext();
        return await context.RiskFlags
            .Where(r => r.Enrollment.ClassId == classId && r.Enrollment.StudentId == studentId)
            .OrderBy(r => r.RiskFlagId)
            .ToListAsync(Ct);
    }

    private async Task AbsentInFinalizedSessionAsync(int classId, int studentId, AttendanceStatus status = AttendanceStatus.Absent)
    {
        await using var context = db.CreateContext();
        var session = new ClassSession
        {
            ClassId = classId, StartTime = Now.AddHours(-2), EndTime = Now.AddHours(-1),
            QrNonce = Guid.NewGuid().ToString("N"), Status = SessionStatus.Finalized
        };
        session.AttendanceRecords.Add(new AttendanceRecord
        {
            StudentId = studentId, Status = status, Source = AttendanceSource.Auto, MarkedAt = Now
        });
        context.ClassSessions.Add(session);
        await context.SaveChangesAsync(Ct);
    }

    private async Task GradeAsync(int classId, int studentId, decimal score, decimal max, decimal weight)
    {
        await using var context = db.CreateContext();
        var assessment = new Assessment
        {
            ClassId = classId, Title = $"A-{Guid.NewGuid():N}", Type = "Quiz", MaxScore = max, WeightPercent = weight
        };
        assessment.Grades.Add(new Grade { StudentId = studentId, Score = score, PostedAt = Now });
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(Ct);
    }

    private sealed class StubSettings(string? threshold, string? passing) : ISystemSettingRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken ct) =>
            Task.FromResult(key == "AbsenceThreshold" ? threshold : key == "PassingGrade" ? passing : null);
    }
}
