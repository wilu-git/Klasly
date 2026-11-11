using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/terms</c> and <c>PUT /api/terms/{id}</c>. Dates as "2026-08-17".</summary>
public class TermRequest
{
    [Required, MaxLength(50)]
    public string Name { get; init; } = string.Empty;

    // Nullable so a missing date is a 400, not a silent 0001-01-01.
    [Required]
    public DateOnly? StartDate { get; init; }

    [Required]
    public DateOnly? EndDate { get; init; }
}
