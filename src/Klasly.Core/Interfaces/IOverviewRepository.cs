using Klasly.Core.DTOs;

namespace Klasly.Core.Interfaces;

/// <summary>Reads a student's dashboard data (Milestone 13). No ownership checks here: the API service does them.</summary>
public interface IOverviewRepository
{
    /// <summary>The student's Active enrollments in the active term, or null when the student doesn't exist.</summary>
    Task<StudentOverviewResponse?> GetAsync(int studentId, CancellationToken ct);
}
