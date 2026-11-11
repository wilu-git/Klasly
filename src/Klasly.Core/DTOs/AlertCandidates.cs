namespace Klasly.Core.DTOs;

/// <summary>One absence email still to queue: one guardian, one Absent record. Times are UTC.</summary>
public record AbsenceAlert(
    string DedupKey,
    int GuardianUserId,
    string GuardianName,
    string StudentName,
    string SubjectCode,
    string SubjectTitle,
    string Section,
    DateTime StartTime,
    DateTime EndTime);

/// <summary>One at-risk email still to queue: one guardian, the enrollment's latest (newly at-risk) RiskFlags row.</summary>
public record AtRiskAlert(
    string DedupKey,
    int GuardianUserId,
    string GuardianName,
    string StudentName,
    string SubjectCode,
    string SubjectTitle,
    string Section,
    decimal AbsenceRate,
    decimal? WeightedAverage);
