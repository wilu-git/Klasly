namespace Klasly.Core.Entities;

/// <summary>
/// dbo.RiskFlags — snapshot written by the worker; dashboards read the latest row per enrollment.
/// </summary>
public class RiskFlag
{
    public int RiskFlagId { get; set; }
    public int EnrollmentId { get; set; }

    /// <summary>Absent / finalized sessions, with Excused excluded.</summary>
    public decimal AbsenceRate { get; set; }

    /// <summary>NULL until a grade is posted.</summary>
    public decimal? WeightedAverage { get; set; }

    /// <summary>AbsenceRate >= 0.20 OR WeightedAverage &lt; 75.</summary>
    public bool IsAtRisk { get; set; }

    /// <summary>UTC.</summary>
    public DateTime ComputedAt { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
}
