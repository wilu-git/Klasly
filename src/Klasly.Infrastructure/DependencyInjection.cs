using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klasly.Infrastructure;

public static class DependencyInjection
{
    /// <summary>The ConnectionStrings key every host reads.</summary>
    public const string ConnectionStringName = "KlaslyDb";

    /// <summary>
    /// Registers <see cref="KlaslyDbContext"/> against SQL Server. Call this from the
    /// API, Web and Worker hosts.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not found. Add it under " +
                $"\"ConnectionStrings\" in appsettings.Development.json, or set it with " +
                $"'dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<value>\"'.");
        }

        services.AddDbContext<KlaslyDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                // Transient network faults are common against hosted SQL; retry them.
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            }));

        return services;
    }
}
