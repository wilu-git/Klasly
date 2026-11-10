namespace Klasly.Core.DTOs;

/// <summary>
/// An assessment of a class. <c>GradedCount</c> counts grades of actively enrolled students; <c>ActiveCount</c> is the class size.
/// <c>DatePostedUtc</c> is when its first grade was saved (null until then).
/// </summary>
public record AssessmentResponse(
    int AssessmentId,
    int ClassId,
    string Title,
    string Type,
    decimal MaxScore,
    decimal WeightPercent,
    DateTime? DatePostedUtc,
    int GradedCount,
    int ActiveCount);
