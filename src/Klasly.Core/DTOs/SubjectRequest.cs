using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/subjects</c> and <c>PUT /api/subjects/{id}</c>.</summary>
public class SubjectRequest
{
    [Required, MaxLength(20)]
    public string Code { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string Title { get; init; } = string.Empty;

    [Range(1, 10)]
    public int Units { get; init; }
}
