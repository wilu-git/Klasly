namespace Klasly.Core.Entities;

/// <summary>The current score of one student on one assessment.</summary>
public class Grade
{
    public int GradeId { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }
    public decimal Score { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
