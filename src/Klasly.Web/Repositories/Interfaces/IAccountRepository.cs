using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<List<AccountResponse>> ListAsync(string? role, CancellationToken ct);
    Task<AccountResponse> GetAsync(int userId, CancellationToken ct);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct);
    Task<AccountResponse> UpdateAsync(int userId, UpdateAccountRequest request, CancellationToken ct);
    Task DeactivateAsync(int userId, CancellationToken ct);
    Task ResetDeviceAsync(int userId, CancellationToken ct);

    Task<List<LinkedStudentResponse>> ListLinkedStudentsAsync(int parentUserId, CancellationToken ct);
    Task<LinkedStudentResponse> LinkStudentAsync(int parentUserId, LinkStudentRequest request, CancellationToken ct);
    Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct);
}
