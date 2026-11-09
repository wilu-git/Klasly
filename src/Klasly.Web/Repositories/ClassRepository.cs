using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class ClassRepository(HttpClient http) : IClassRepository
{
    public Task<List<ClassResponse>> ListAsync(int? termId, CancellationToken ct) =>
        http.GetApiAsync<List<ClassResponse>>(termId is null ? "api/classes" : $"api/classes?termId={termId}", ct);

    public Task<ClassResponse> GetAsync(int id, CancellationToken ct) => http.GetApiAsync<ClassResponse>($"api/classes/{id}", ct);

    public Task<ClassResponse> CreateAsync(ClassRequest request, CancellationToken ct) =>
        http.PostApiAsync<ClassResponse>("api/classes", request, ct);

    public Task<ClassResponse> UpdateAsync(int id, ClassRequest request, CancellationToken ct) =>
        http.PutApiAsync<ClassResponse>($"api/classes/{id}", request, ct);

    public Task<List<EnrollmentResponse>> ListEnrollmentsAsync(int classId, CancellationToken ct) =>
        http.GetApiAsync<List<EnrollmentResponse>>($"api/classes/{classId}/enrollments", ct);

    // 201 for a new enrollment, 200 when a Dropped one is set back to Active; both are success here.
    public Task<EnrollmentResponse> EnrollAsync(EnrollmentRequest request, CancellationToken ct) =>
        http.PostApiAsync<EnrollmentResponse>("api/enrollments", request, ct);

    public Task DropAsync(int enrollmentId, CancellationToken ct) => http.DeleteApiAsync($"api/enrollments/{enrollmentId}", ct);
}
