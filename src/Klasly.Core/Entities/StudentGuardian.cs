namespace Klasly.Core.Entities;

/// <summary>dbo.StudentGuardians — M:N join between students and guardians.</summary>
public class StudentGuardian
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }

    /// <summary>Mother, Father, Guardian...</summary>
    public string? Relationship { get; set; }

    public bool ReceivesAlerts { get; set; } = true;

    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}
