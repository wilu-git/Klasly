using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>
/// Body of <c>POST /api/classes/{classId}/assessments</c> and <c>PUT /api/assessments/{id}</c>.
/// The class weight total (≤ 100%) and "max ≥ every posted score" are checked in the service.
/// </summary>
public class AssessmentRequest
{
    [Required, MaxLength(100)]
    public string Title { get; init; } = string.Empty;

    /// <summary>Free text, e.g. Quiz, Exam, Activity, Project, Recitation.</summary>
    [Required, MaxLength(30)]
    public string Type { get; init; } = string.Empty;

    [Required, Range(0.01, 9999.99)]
    public decimal? MaxScore { get; init; }

    [Required, Range(0.01, 100)]
    public decimal? WeightPercent { get; init; }
}
