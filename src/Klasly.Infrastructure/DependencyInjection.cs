using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Klasly.Infrastructure;

/// <summary>Registers the EF Core context (and later the repositories) for the Web API and the Worker.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<KlaslyDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
