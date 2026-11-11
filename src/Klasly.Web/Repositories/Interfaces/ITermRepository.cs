using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

public interface ITermRepository
{
    Task<List<TermResponse>> ListAsync(CancellationToken ct);
    Task<TermResponse> GetAsync(int id, CancellationToken ct);
    Task<TermResponse> CreateAsync(TermRequest request, CancellationToken ct);
    Task<TermResponse> UpdateAsync(int id, TermRequest request, CancellationToken ct);
    Task<TermResponse> ActivateAsync(int id, CancellationToken ct);
    Task DeactivateAsync(int id, CancellationToken ct);
}
