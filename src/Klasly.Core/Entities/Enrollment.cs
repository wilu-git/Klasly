using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.Enrollments — M:N join between students and class offerings.</summary>
public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public ClassOffering Class { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<RiskFlag> RiskFlags { get; set; } = new List<RiskFlag>();
}
