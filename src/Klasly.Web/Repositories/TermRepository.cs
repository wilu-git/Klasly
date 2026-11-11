using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class TermRepository(HttpClient http) : ITermRepository
{
    public Task<List<TermResponse>> ListAsync(CancellationToken ct) => http.GetApiAsync<List<TermResponse>>("api/terms", ct);

    public Task<TermResponse> GetAsync(int id, CancellationToken ct) => http.GetApiAsync<TermResponse>($"api/terms/{id}", ct);

    public Task<TermResponse> CreateAsync(TermRequest request, CancellationToken ct) =>
        http.PostApiAsync<TermResponse>("api/terms", request, ct);

    public Task<TermResponse> UpdateAsync(int id, TermRequest request, CancellationToken ct) =>
        http.PutApiAsync<TermResponse>($"api/terms/{id}", request, ct);

    public Task<TermResponse> ActivateAsync(int id, CancellationToken ct) =>
        http.PostApiAsync<TermResponse>($"api/terms/{id}/activate", null, ct);

    public Task DeactivateAsync(int id, CancellationToken ct) => http.DeleteApiAsync($"api/terms/{id}", ct);
}
