using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for assessments. (ClassId, Title) is unique; assessments are never deleted.</summary>
public interface IAssessmentRepository
{
    /// <summary>Not tracked, oldest first.</summary>
    Task<List<Assessment>> ListByClassAsync(int classId, CancellationToken ct);

    /// <summary>Tracked, with Class and Class.Subject loaded.</summary>
    Task<Assessment?> GetByIdAsync(int assessmentId, CancellationToken ct);

    /// <summary>Total WeightPercent of the class, leaving out <paramref name="excludeAssessmentId"/> (the one being edited).</summary>
    Task<decimal> SumWeightsAsync(int classId, int? excludeAssessmentId, CancellationToken ct);

    Task<bool> TitleExistsAsync(int classId, string title, int? excludeAssessmentId, CancellationToken ct);

    /// <summary>The highest score posted on the assessment, or null when nothing is graded.</summary>
    Task<decimal?> MaxPostedScoreAsync(int assessmentId, CancellationToken ct);

    /// <summary>Grades of actively enrolled students per assessment of the class.</summary>
    Task<Dictionary<int, int>> CountGradedAsync(int classId, CancellationToken ct);

    Task AddAsync(Assessment assessment, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
