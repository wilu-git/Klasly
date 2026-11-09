namespace Klasly.Core.Entities;

/// <summary>Teacher profile, one-to-one with <see cref="User"/>; owns classes.</summary>
public class Teacher
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string? Department { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Class> Classes { get; set; } = [];
}
