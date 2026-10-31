namespace Klasly.Core.Entities;

/// <summary>dbo.Teachers — 1-to-1 with <see cref="User"/>.</summary>
public class Teacher
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string EmployeeNo { get; set; } = null!;
    public string? Department { get; set; }

    public User User { get; set; } = null!;
    public ICollection<ClassOffering> Classes { get; set; } = new List<ClassOffering>();
}
