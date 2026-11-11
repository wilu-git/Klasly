using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class AssessmentRepository(HttpClient http) : IAssessmentRepository
{
    // Oldest first (API order).
    public Task<List<AssessmentResponse>> ListByClassAsync(int classId, CancellationToken ct) =>
        http.GetApiAsync<List<AssessmentResponse>>($"api/classes/{classId}/assessments", ct);

    public Task<AssessmentResponse> GetAsync(int assessmentId, CancellationToken ct) =>
        http.GetApiAsync<AssessmentResponse>($"api/assessments/{assessmentId}", ct);

    // 400 with field errors (e.g. weightPercent over 100% in total); 409 when the title is taken in the class.
    public Task<AssessmentResponse> CreateAsync(int classId, AssessmentRequest request, CancellationToken ct) =>
        http.PostApiAsync<AssessmentResponse>($"api/classes/{classId}/assessments", request, ct);

    public Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentRequest request, CancellationToken ct) =>
        http.PutApiAsync<AssessmentResponse>($"api/assessments/{assessmentId}", request, ct);

    // Every actively enrolled student by name; Score null until graded.
    public Task<List<GradeRow>> GetGradesAsync(int assessmentId, CancellationToken ct) =>
        http.GetApiAsync<List<GradeRow>>($"api/assessments/{assessmentId}/grades", ct);

    // 400 with row errors keyed grades[i].score, nothing saved; 409 when another save won a race.
    public Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, SaveGradesRequest request, CancellationToken ct) =>
        http.PutApiAsync<SaveGradesResponse>($"api/assessments/{assessmentId}/grades", request, ct);
}
