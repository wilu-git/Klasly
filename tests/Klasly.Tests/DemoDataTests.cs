using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;
using Klasly.Worker.Services;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// deploy/demo-data.sql on a freshly migrated database, the same as on the VPS (efbundle, then the script).
/// It gets its own database because the script's numbers only hold when no other test has touched classes 1 and 2.
/// </summary>
public class DemoDataTests
{
    private const string DatabaseName = "Klasly_DemoData_Tests";
    private const string ConnectionString =
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Script_gives_the_spec_risk_mix_and_a_second_run_adds_nothing()
    {
        await using (var context = CreateContext())
        {
            await context.Database.EnsureDeletedAsync(Ct);
            await context.Database.MigrateAsync(Ct);
            await context.Database.ExecuteSqlRawAsync(Script(), Ct);
            await new RiskCalculator(new RiskRepository(context), new SystemSettingRepository(context)).RecomputeAsync(DateTime.UtcNow, Ct);
        }

        await using (var context = CreateContext())
        {
            var flags = await context.RiskFlags
                .OrderBy(f => f.Enrollment.ClassId).ThenBy(f => f.Enrollment.StudentId)
                .Select(f => new { f.Enrollment.ClassId, f.Enrollment.StudentId, f.AbsenceRate, f.WeightedAverage, f.IsAtRisk })
                .ToListAsync(Ct);

            // SPEC.md "Expected risk result": (class, student, absence rate, weighted average, at risk)
            Assert.Equal(
            [
                (1, 1, 0.0000m, 89.50m, false), // Aira
                (1, 2, 0.3333m, 79.50m, true),  // Bryan: 4 of 12 absent
                (1, 3, 0.0833m, 65.75m, true),  // Carla: grades
                (1, 4, 0.1667m, 79.25m, false), // Dennis: one more absence flips him
                (1, 5, 0.0000m, 93.25m, false), // Ellen
                (1, 6, 0.0833m, 80.00m, false), // Francis
                (2, 1, 0.0000m, 85.00m, false),
                (2, 2, 0.1250m, 76.80m, false),
                (2, 3, 0.0000m, 80.00m, false),
            ], flags.Select(f => (f.ClassId, f.StudentId, f.AbsenceRate, f.WeightedAverage!.Value, f.IsAtRisk)));

            Assert.Equal(12, await context.ClassSessions.CountAsync(s => s.ClassId == 1, Ct));
            Assert.Equal(8, await context.ClassSessions.CountAsync(s => s.ClassId == 2, Ct));
            Assert.Equal(3, await context.AttendanceRecords.CountAsync(r => r.Status == Core.Entities.AttendanceStatus.Late && r.Session.ClassId == 1, Ct)); // Aira 1 + Ellen 2
        }

        await using (var context = CreateContext())
        {
            var before = (await context.ClassSessions.CountAsync(Ct), await context.AttendanceRecords.CountAsync(Ct), await context.Grades.CountAsync(Ct));
            await context.Database.ExecuteSqlRawAsync(Script(), Ct);
            var after = (await context.ClassSessions.CountAsync(Ct), await context.AttendanceRecords.CountAsync(Ct), await context.Grades.CountAsync(Ct));
            Assert.Equal(before, after);
        }
    }

    private static KlaslyDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<KlaslyDbContext>().UseSqlServer(ConnectionString).Options);

    private static string Script()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Klasly.slnx"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Repo root not found."), "deploy", "demo-data.sql"));
    }
}
