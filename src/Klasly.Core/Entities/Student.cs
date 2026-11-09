namespace Klasly.Core.Entities;

/// <summary>dbo.Students — 1-to-1 with <see cref="User"/>.</summary>
public class Student
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string StudentNumber { get; set; } = null!;
    public int YearLevel { get; set; }

    /// <summary>e.g. BSCpE.</summary>
    public string Program { get; set; } = null!;

    public User User { get; set; } = null!;
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
