using Klasly.Core.DTOs;
using Klasly.Core.Entities;

namespace Klasly.API.Services.Interfaces;

public interface IAccountService
{
    Task<List<AccountResponse>> ListAsync(UserRole? role, CancellationToken ct);
    Task<AccountResponse> GetAsync(int userId, CancellationToken ct);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct);

    /// <param name="currentUserId">The admin making the call; they cannot deactivate themselves.</param>
    Task<AccountResponse> UpdateAsync(int userId, UpdateAccountRequest request, int currentUserId, CancellationToken ct);
    Task DeactivateAsync(int userId, int currentUserId, CancellationToken ct);

    Task<List<LinkedStudentResponse>> ListLinkedStudentsAsync(int parentUserId, CancellationToken ct);
    Task<LinkedStudentResponse> LinkStudentAsync(int parentUserId, LinkStudentRequest request, CancellationToken ct);
    Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct);
}
