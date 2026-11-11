namespace Klasly.Core.DTOs;

/// <summary>
/// What the Worker's risk step needs for one active enrollment, loaded in one query.
/// <c>Latest</c> is the newest RiskFlags snapshot, or null when the enrollment has none yet.
/// </summary>
public record RiskInput(
    int EnrollmentId,
    int AbsentCount,
    int CountedCount,
    decimal WeightedSum,
    decimal WeightSum,
    RiskSnapshot? Latest);

/// <summary>The values of one stored RiskFlags row, used to skip writing an unchanged snapshot.</summary>
public record RiskSnapshot(decimal AbsenceRate, decimal? WeightedAverage, bool IsAtRisk);
