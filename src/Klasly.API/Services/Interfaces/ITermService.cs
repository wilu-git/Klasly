using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface ITermService
{
    Task<List<TermResponse>> ListAsync(CancellationToken ct);
    Task<TermResponse> GetAsync(int termId, CancellationToken ct);
    Task<TermResponse> CreateAsync(TermRequest request, CancellationToken ct);
    Task<TermResponse> UpdateAsync(int termId, TermRequest request, CancellationToken ct);
    Task<TermResponse> ActivateAsync(int termId, CancellationToken ct);
    Task DeactivateAsync(int termId, CancellationToken ct);
}
