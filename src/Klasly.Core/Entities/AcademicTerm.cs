namespace Klasly.Core.Entities;

/// <summary>dbo.AcademicTerms — only one row may have IsActive = 1.</summary>
public class AcademicTerm
{
    public int TermId { get; set; }

    /// <summary>e.g. 1st Sem 2026-27.</summary>
    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ClassOffering> Classes { get; set; } = new List<ClassOffering>();
}
