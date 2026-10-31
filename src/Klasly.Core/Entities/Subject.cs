namespace Klasly.Core.Entities;

/// <summary>dbo.Subjects — the catalogue entry, independent of term or section.</summary>
public class Subject
{
    public int SubjectId { get; set; }

    /// <summary>e.g. ELECT3.</summary>
    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;
    public int Units { get; set; }

    public ICollection<ClassOffering> Classes { get; set; } = new List<ClassOffering>();
}
