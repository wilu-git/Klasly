namespace Klasly.Core.Entities;

/// <summary>dbo.Assessments — a graded activity inside a class offering.</summary>
public class Assessment
{
    public int AssessmentId { get; set; }
    public int ClassId { get; set; }

    /// <summary>e.g. Quiz 1, Midterm Exam.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Free-form in the schema (no CHECK constraint).</summary>
    public string Type { get; set; } = null!;

    public decimal MaxScore { get; set; }

    /// <summary>Per class these should total 100; enforced in the API, not the database.</summary>
    public decimal WeightPercent { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? DatePosted { get; set; }

    public ClassOffering Class { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
