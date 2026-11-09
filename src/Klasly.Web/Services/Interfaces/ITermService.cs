using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface ITermService
{
    Task<List<TermResponse>> ListAsync(CancellationToken ct);
    Task<TermFormViewModel> GetFormAsync(int id, CancellationToken ct);
    Task CreateAsync(TermFormViewModel model, CancellationToken ct);
    Task UpdateAsync(TermFormViewModel model, CancellationToken ct);
    Task<TermResponse> ActivateAsync(int id, CancellationToken ct);
    Task DeactivateAsync(int id, CancellationToken ct);
}
