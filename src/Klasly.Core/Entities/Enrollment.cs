namespace Klasly.Core.Entities;

/// <summary>A seat of a student in a class. Dropping sets <see cref="Status"/> to Dropped; rows are never deleted.</summary>
public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public Class Class { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<RiskFlag> RiskFlags { get; set; } = [];
}
