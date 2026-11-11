using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>One actively enrolled student on an assessment's grade sheet. <c>Score</c> null = not graded yet.</summary>
public record GradeRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    decimal? Score,
    DateTime? PostedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>Body of <c>PUT /api/assessments/{id}/grades</c>: the whole sheet in one go; a null score is skipped.</summary>
public class SaveGradesRequest
{
    [Required]
    public List<GradeEntry> Grades { get; init; } = [];
}

/// <summary>One row of <see cref="SaveGradesRequest"/>. 0 ≤ Score ≤ MaxScore, 2 decimals at most (checked in the service).</summary>
public class GradeEntry
{
    public int StudentId { get; init; }
    public decimal? Score { get; init; }
}

/// <summary>How many grades were new or changed, and the sheet after the save.</summary>
public record SaveGradesResponse(int Added, int Changed, List<GradeRow> Grades);
