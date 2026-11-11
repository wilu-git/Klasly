using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/classes</c> and <c>PUT /api/classes/{id}</c>.</summary>
public class ClassRequest
{
    [Range(1, int.MaxValue)]
    public int SubjectId { get; init; }

    [Range(1, int.MaxValue)]
    public int TeacherId { get; init; }

    [Range(1, int.MaxValue)]
    public int TermId { get; init; }

    [Required, MaxLength(30)]
    public string Section { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Schedule { get; init; }

    [MaxLength(30)]
    public string? Room { get; init; }
}
