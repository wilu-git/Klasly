namespace Klasly.Core.Entities;

/// <summary>A semester. Only one term may have <see cref="IsActive"/> set at a time.</summary>
public class AcademicTerm
{
    public int TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Class> Classes { get; set; } = [];
}
