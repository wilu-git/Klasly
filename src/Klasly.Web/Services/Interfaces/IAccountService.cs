using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface IAccountService
{
    Task<List<AccountResponse>> ListAsync(string? role, CancellationToken ct);
    Task<AccountFormViewModel> GetFormAsync(int userId, CancellationToken ct);
    Task<ParentLinksViewModel> GetParentLinksAsync(int parentUserId, CancellationToken ct);
    Task CreateAsync(AccountFormViewModel model, CancellationToken ct);
    Task UpdateAsync(AccountFormViewModel model, CancellationToken ct);
    Task DeactivateAsync(int userId, CancellationToken ct);
    Task LinkStudentAsync(int parentUserId, int studentId, string? relationship, bool receivesAlerts, CancellationToken ct);
    Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct);
}
