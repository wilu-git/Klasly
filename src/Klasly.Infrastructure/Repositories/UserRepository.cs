using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class UserRepository(KlaslyDbContext context) : IUserRepository
{
    // Email is unique (UQ_Users_Email); the default SQL Server collation makes the match case-insensitive.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
}
