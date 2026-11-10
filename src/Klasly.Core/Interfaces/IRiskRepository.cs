using Klasly.Core.DTOs;
using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for the Worker's risk step (guide §27.8). RiskFlags rows are history: added, never changed.</summary>
public interface IRiskRepository
{
    /// <summary>
    /// One row per Active enrollment in a class of the active term: the student's Absent and counted
    /// (Present + Late + Absent) records in that class's Finalized sessions, the weighted sums of the student's
    /// grades in that class, and the latest snapshot. One database round trip.
    /// </summary>
    Task<List<RiskInput>> ListInputsAsync(CancellationToken ct);

    /// <summary>Tracks the row; it is written by the next save on the same context.</summary>
    void Add(RiskFlag flag);

    Task SaveChangesAsync(CancellationToken ct);
}
