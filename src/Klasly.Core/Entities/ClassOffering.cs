namespace Klasly.Core.Entities;

/// <summary>
/// dbo.Classes — one subject offering for one section in one term.
/// Named ClassOffering because "class" is a C# keyword; it maps to the Classes table.
/// </summary>
public class ClassOffering
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public int TermId { get; set; }

    /// <summary>e.g. BSCpE 3-A.</summary>
    public string Section { get; set; } = null!;

    /// <summary>e.g. MWF 07:30-09:00 (PH time).</summary>
    public string? Schedule { get; set; }

    public string? Room { get; set; }

    public Subject Subject { get; set; } = null!;
    public Teacher Teacher { get; set; } = null!;
    public AcademicTerm Term { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<ClassSession> Sessions { get; set; } = new List<ClassSession>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
