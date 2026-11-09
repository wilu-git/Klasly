namespace Klasly.Core.Entities;

/// <summary>A graded activity of a class; weights per class must total 100% (checked in the Web API).</summary>
public class Assessment
{
    public int AssessmentId { get; set; }
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal WeightPercent { get; set; }
    public DateTime? DatePosted { get; set; }

    public Class Class { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = [];
}
