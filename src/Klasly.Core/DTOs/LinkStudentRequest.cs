using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/accounts/{userId}/students</c>: links a parent to a student.</summary>
public class LinkStudentRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    [MaxLength(30)]
    public string? Relationship { get; init; }

    public bool ReceivesAlerts { get; init; } = true;
}
