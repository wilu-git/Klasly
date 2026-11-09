namespace Klasly.Core.Entities;

/// <summary>Join row linking a student to a guardian (composite key, M:N).</summary>
public class StudentGuardian
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }
    public string? Relationship { get; set; }
    public bool ReceivesAlerts { get; set; } = true;

    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}
