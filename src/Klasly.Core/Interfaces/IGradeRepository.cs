using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for grades. (AssessmentId, StudentId) is unique: one current score per student.</summary>
public interface IGradeRepository
{
    /// <summary>Tracked, so the service can change scores in place.</summary>
    Task<List<Grade>> ListByAssessmentAsync(int assessmentId, CancellationToken ct);

    /// <summary>Tracks the grade; it is written by the next <see cref="TrySaveAsync"/>.</summary>
    void Add(Grade grade);

    /// <summary>
    /// Saves every pending change (grades, the assessment's DatePosted, push rows) in one transaction. False, with nothing
    /// saved and the change tracker cleared, when another request inserted a grade for the same student first.
    /// </summary>
    Task<bool> TrySaveAsync(CancellationToken ct);
}
