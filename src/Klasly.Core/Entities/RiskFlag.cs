namespace Klasly.Core.Entities;

/// <summary>Early-warning snapshot written by the Worker; dashboards read the latest row per enrollment.</summary>
public class RiskFlag
{
    public int RiskFlagId { get; set; }
    public int EnrollmentId { get; set; }

    /// <summary>Absent / finalized sessions, 0 to 1 (Excused excluded).</summary>
    public decimal AbsenceRate { get; set; }

    /// <summary>Null until a grade is posted.</summary>
    public decimal? WeightedAverage { get; set; }

    public bool IsAtRisk { get; set; }
    public DateTime ComputedAt { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
}
