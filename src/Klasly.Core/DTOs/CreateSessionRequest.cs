using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/classes/{classId}/sessions</c>. Times carry their offset, e.g. 2026-11-10T09:00:00+08:00.</summary>
public class CreateSessionRequest
{
    [Required]
    public DateTimeOffset? StartTime { get; init; }

    [Required]
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>Minutes after the start when a scan counts as Late instead of Present.</summary>
    [Range(0, 120)]
    public int LateAfterMinutes { get; init; } = 15;
}
