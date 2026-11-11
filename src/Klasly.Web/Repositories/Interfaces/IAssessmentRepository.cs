using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>The teacher's assessment and grade API calls. The API checks the class is the teacher's (403 otherwise).</summary>
public interface IAssessmentRepository
{
    Task<List<AssessmentResponse>> ListByClassAsync(int classId, CancellationToken ct);
    Task<AssessmentResponse> GetAsync(int assessmentId, CancellationToken ct);
    Task<AssessmentResponse> CreateAsync(int classId, AssessmentRequest request, CancellationToken ct);
    Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentRequest request, CancellationToken ct);
    Task<List<GradeRow>> GetGradesAsync(int assessmentId, CancellationToken ct);
    Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, SaveGradesRequest request, CancellationToken ct);
}
