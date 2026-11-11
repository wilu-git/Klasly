using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>
/// A teacher's class page: the class header, the Start session form and its sessions (newest first),
/// and its assessments (oldest first) with the Add assessment form.
/// </summary>
public record TeacherClassViewModel(ClassResponse Class, List<SessionResponse> Sessions)
{
    public StartSessionViewModel Start { get; init; } = new();
    public List<AssessmentResponse> Assessments { get; init; } = [];
    public AssessmentFormViewModel NewAssessment { get; init; } = new();
    public decimal WeightTotal => Assessments.Sum(a => a.WeightPercent);
}
