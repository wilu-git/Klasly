using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

/// <summary>A teacher's assessments. Every method checks that the caller owns the class.</summary>
public interface IAssessmentService
{
    Task<List<AssessmentResponse>> ListAsync(int classId, int userId, CancellationToken ct);
    Task<AssessmentResponse> GetAsync(int assessmentId, int userId, CancellationToken ct);
    Task<AssessmentResponse> CreateAsync(int classId, AssessmentRequest request, int userId, CancellationToken ct);
    Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentRequest request, int userId, CancellationToken ct);

    /// <summary>The grade sheet: every actively enrolled student by name, score null until graded.</summary>
    Task<List<GradeRow>> GetGradesAsync(int assessmentId, int userId, CancellationToken ct);

    /// <summary>Saves a whole sheet; one invalid row → 400 with row errors and nothing saved.</summary>
    Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, SaveGradesRequest request, int userId, CancellationToken ct);
}
