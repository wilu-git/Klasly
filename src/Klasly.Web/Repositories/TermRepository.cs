using System.Net.Http.Json;
using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class TermRepository(HttpClient http) : ITermRepository
{
    public async Task<List<TermResponse>> ListAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/terms", ct);
        return await ApiResponse.ReadAsync<List<TermResponse>>(response, ct);
    }

    public async Task<TermResponse> GetAsync(int id, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/terms/{id}", ct);
        return await ApiResponse.ReadAsync<TermResponse>(response, ct);
    }

    public async Task<TermResponse> CreateAsync(TermRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/terms", request, ct);
        return await ApiResponse.ReadAsync<TermResponse>(response, ct);
    }

    public async Task<TermResponse> UpdateAsync(int id, TermRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/terms/{id}", request, ct);
        return await ApiResponse.ReadAsync<TermResponse>(response, ct);
    }

    public async Task<TermResponse> ActivateAsync(int id, CancellationToken ct)
    {
        using var response = await http.PostAsync($"api/terms/{id}/activate", null, ct);
        return await ApiResponse.ReadAsync<TermResponse>(response, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/terms/{id}", ct);
        await ApiResponse.EnsureAsync(response, ct);
    }
}
