namespace Klasly.Core.Entities;

/// <summary>Student profile, one-to-one with <see cref="User"/>.</summary>
public class Student
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string Program { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
    public ICollection<Grade> Grades { get; set; } = [];
}
