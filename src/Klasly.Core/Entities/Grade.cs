namespace Klasly.Core.Entities;

/// <summary>dbo.Grades — one score per student per assessment.</summary>
public class Grade
{
    public int GradeId { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }

    /// <summary>The upper bound against Assessment.MaxScore is checked in the API.</summary>
    public decimal Score { get; set; }

    /// <summary>UTC.</summary>
    public DateTime PostedAt { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? UpdatedAt { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
