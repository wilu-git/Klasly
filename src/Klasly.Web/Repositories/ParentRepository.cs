using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class ParentRepository(HttpClient http) : IParentRepository
{
    // Ordered by name (API order). Only this parent's linked students.
    public Task<List<LinkedStudentResponse>> ListChildrenAsync(CancellationToken ct) =>
        http.GetApiAsync<List<LinkedStudentResponse>>("api/parent/children", ct);

    // 403 for a student who isn't linked to this parent.
    public Task<StudentOverviewResponse> GetOverviewAsync(int studentId, CancellationToken ct) =>
        http.GetApiAsync<StudentOverviewResponse>($"api/parent/children/{studentId}/overview", ct);
}
