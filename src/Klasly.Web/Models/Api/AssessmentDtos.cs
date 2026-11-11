namespace Klasly.Web.Models.Api;

/// <summary>Body of <c>POST /api/classes/{classId}/assessments</c> and <c>PUT /api/assessments/{id}</c>.</summary>
public record AssessmentRequest(string Title, string Type, decimal? MaxScore, decimal? WeightPercent);

/// <summary>An assessment of a class. GradedCount = graded active students; ActiveCount = class size.</summary>
public record AssessmentResponse(
    int AssessmentId, int ClassId, string Title, string Type, decimal MaxScore, decimal WeightPercent,
    DateTime? DatePostedUtc, int GradedCount, int ActiveCount);

/// <summary>One active student on the grade sheet; Score null = not graded yet.</summary>
public record GradeRow(int StudentId, string StudentNumber, string FullName, decimal? Score, DateTime? PostedAtUtc, DateTime? UpdatedAtUtc);

/// <summary>Body of <c>PUT /api/assessments/{id}/grades</c>; a null score is skipped.</summary>
public record SaveGradesRequest(List<GradeEntry> Grades);

public record GradeEntry(int StudentId, decimal? Score);

public record SaveGradesResponse(int Added, int Changed, List<GradeRow> Grades);
