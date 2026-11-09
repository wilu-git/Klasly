namespace Klasly.Core.Entities;

/// <summary>Course catalog entry (code, title, units).</summary>
public class Subject
{
    public int SubjectId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Units { get; set; }

    public ICollection<Class> Classes { get; set; } = [];
}
