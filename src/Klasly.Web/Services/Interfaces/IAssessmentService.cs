using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface IAssessmentService
{
    /// <summary>The assessment page; <paramref name="postedGrades"/> (after a failed save) replaces the scores from the API.</summary>
    Task<AssessmentPageViewModel> GetPageAsync(int assessmentId, CancellationToken ct, List<GradeInputViewModel>? postedGrades = null);

    Task<AssessmentResponse> CreateAsync(AssessmentFormViewModel model, CancellationToken ct);
    Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentFormViewModel model, CancellationToken ct);
    Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, List<GradeInputViewModel> grades, CancellationToken ct);
}
