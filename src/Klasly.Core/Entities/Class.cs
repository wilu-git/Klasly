namespace Klasly.Core.Entities;

/// <summary>One subject offering for one section in one term, owned by a teacher.</summary>
public class Class
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public int TermId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string? Schedule { get; set; }
    public string? Room { get; set; }

    public Subject Subject { get; set; } = null!;
    public Teacher Teacher { get; set; } = null!;
    public AcademicTerm Term { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<ClassSession> ClassSessions { get; set; } = [];
    public ICollection<Assessment> Assessments { get; set; } = [];
}
