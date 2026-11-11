using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

/// <summary>Shapes the assessment page from the API calls. No business rules here (the API decides).</summary>
public class AssessmentService(IAssessmentRepository assessments, ISessionRepository sessions) : IAssessmentService
{
    public async Task<AssessmentPageViewModel> GetPageAsync(
        int assessmentId, CancellationToken ct, List<GradeInputViewModel>? postedGrades = null)
    {
        // The assessment first: the API answers 403/404 for another teacher's, before we look for the class header.
        var assessment = await assessments.GetAsync(assessmentId, ct);
        var cls = (await sessions.ListMyClassesAsync(ct)).FirstOrDefault(c => c.ClassId == assessment.ClassId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Class not found.", new Dictionary<string, string[]>());

        var posted = postedGrades?.ToDictionary(g => g.StudentId, g => g.Score);
        var grades = (await assessments.GetGradesAsync(assessmentId, ct)).Select(g => new GradeInputViewModel
        {
            StudentId = g.StudentId,
            Score = posted is null ? g.Score : posted.GetValueOrDefault(g.StudentId),
            StudentNumber = g.StudentNumber,
            FullName = g.FullName,
            LastSavedUtc = g.UpdatedAtUtc ?? g.PostedAtUtc
        }).ToList();

        return new AssessmentPageViewModel(cls, assessment, grades)
        {
            Edit = new AssessmentFormViewModel
            {
                ClassId = assessment.ClassId,
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                Type = assessment.Type,
                MaxScore = assessment.MaxScore,
                WeightPercent = assessment.WeightPercent
            }
        };
    }

    public Task<AssessmentResponse> CreateAsync(AssessmentFormViewModel model, CancellationToken ct) =>
        assessments.CreateAsync(model.ClassId, ToRequest(model), ct);

    public Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentFormViewModel model, CancellationToken ct) =>
        assessments.UpdateAsync(assessmentId, ToRequest(model), ct);

    public Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, List<GradeInputViewModel> grades, CancellationToken ct) =>
        assessments.SaveGradesAsync(assessmentId, new SaveGradesRequest(grades.Select(g => new GradeEntry(g.StudentId, g.Score)).ToList()), ct);

    private static AssessmentRequest ToRequest(AssessmentFormViewModel m) => new(m.Title, m.Type, m.MaxScore, m.WeightPercent);
}
