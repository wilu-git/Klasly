using Microsoft.EntityFrameworkCore;

namespace Klasly.Worker.Tests;

public class DatabaseFixtureTests(DatabaseFixture db)
{
    [Fact]
    public async Task Migrated_test_database_has_the_seeded_settings()
    {
        await using var context = db.CreateContext();

        Assert.Equal("Klasly_Tests", context.Database.GetDbConnection().Database);
        Assert.Equal(5, await context.SystemSettings.CountAsync(TestContext.Current.CancellationToken));
    }
}
