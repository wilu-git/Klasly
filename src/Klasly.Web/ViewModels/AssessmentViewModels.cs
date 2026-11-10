using System.ComponentModel.DataAnnotations;
using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>The add/edit assessment form. The API checks the class weight total and max ≥ posted scores.</summary>
public class AssessmentFormViewModel
{
    public int ClassId { get; set; }

    /// <summary>Null on the class page (add); set on the assessment page (edit).</summary>
    public int? AssessmentId { get; set; }

    [Required(ErrorMessage = "Enter a title."), StringLength(100), Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a type."), StringLength(30), Display(Name = "Type")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the max score."), Range(0.01, 9999.99, ErrorMessage = "Use 0.01 to 9999.99."), Display(Name = "Max score")]
    public decimal? MaxScore { get; set; }

    [Required(ErrorMessage = "Enter the weight."), Range(0.01, 100, ErrorMessage = "Use 0.01 to 100."), Display(Name = "Weight (%)")]
    public decimal? WeightPercent { get; set; }

    public static readonly string[] SuggestedTypes = ["Quiz", "Exam", "Activity", "Project", "Recitation"];
}

/// <summary>One row of the grade sheet form. Only StudentId and Score are posted; the rest is for display.</summary>
public class GradeInputViewModel
{
    public int StudentId { get; set; }
    public decimal? Score { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? LastSavedUtc { get; set; }
}

/// <summary>The assessment page: its class (for the breadcrumb), the grade sheet and the edit form.</summary>
public record AssessmentPageViewModel(ClassResponse Class, AssessmentResponse Assessment, List<GradeInputViewModel> Grades)
{
    public AssessmentFormViewModel Edit { get; init; } = new();
    public int Graded => Grades.Count(g => g.Score is not null);
}
