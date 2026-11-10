using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

[assembly: AssemblyFixture(typeof(Klasly.Worker.Tests.DatabaseFixture))]

namespace Klasly.Worker.Tests;

/// <summary>
/// One LocalDB test database for the whole test run, built by the real migrations (real CHECKs and unique indexes).
/// It is dropped and re-created at the start of each run. Never the developer's Klasly database.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "Klasly_Tests";

    // Windows auth, no password: safe to keep in code.
    private const string ConnectionString =
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    public KlaslyDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<KlaslyDbContext>().UseSqlServer(ConnectionString).Options);

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateContext();
        // Guard before the drop: this fixture must never delete anything but the test database.
        if (context.Database.GetDbConnection().Database != DatabaseName)
            throw new InvalidOperationException($"Refusing to reset '{context.Database.GetDbConnection().Database}'.");

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
