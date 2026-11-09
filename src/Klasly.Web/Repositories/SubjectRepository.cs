using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class SubjectRepository(HttpClient http) : ISubjectRepository
{
    public Task<List<SubjectResponse>> ListAsync(CancellationToken ct) =>
        http.GetApiAsync<List<SubjectResponse>>("api/subjects", ct);

    public Task<SubjectResponse> GetAsync(int id, CancellationToken ct) =>
        http.GetApiAsync<SubjectResponse>($"api/subjects/{id}", ct);

    public Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct) =>
        http.PostApiAsync<SubjectResponse>("api/subjects", request, ct);

    public Task<SubjectResponse> UpdateAsync(int id, SubjectRequest request, CancellationToken ct) =>
        http.PutApiAsync<SubjectResponse>($"api/subjects/{id}", request, ct);
}
