using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

public class TermService(ITermRepository terms) : ITermService
{
    public Task<List<TermResponse>> ListAsync(CancellationToken ct) => terms.ListAsync(ct);

    public async Task<TermFormViewModel> GetFormAsync(int id, CancellationToken ct)
    {
        var term = await terms.GetAsync(id, ct);
        return new TermFormViewModel { Id = term.TermId, Name = term.Name, StartDate = term.StartDate, EndDate = term.EndDate };
    }

    public Task CreateAsync(TermFormViewModel model, CancellationToken ct) => terms.CreateAsync(ToRequest(model), ct);

    public Task UpdateAsync(TermFormViewModel model, CancellationToken ct) =>
        terms.UpdateAsync(model.Id!.Value, ToRequest(model), ct);

    public Task<TermResponse> ActivateAsync(int id, CancellationToken ct) => terms.ActivateAsync(id, ct);

    public Task DeactivateAsync(int id, CancellationToken ct) => terms.DeactivateAsync(id, ct);

    private static TermRequest ToRequest(TermFormViewModel model) => new(model.Name, model.StartDate, model.EndDate);
}
