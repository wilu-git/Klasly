using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for login accounts. Business rules (active check, password) stay in the services.</summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
}
