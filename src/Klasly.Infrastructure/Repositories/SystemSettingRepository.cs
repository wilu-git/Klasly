using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ISystemSettingRepository"/>.</summary>
public class SystemSettingRepository(KlaslyDbContext context) : ISystemSettingRepository
{
    public Task<string?> GetValueAsync(string key, CancellationToken ct) =>
        context.SystemSettings.AsNoTracking().Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync(ct);
}
