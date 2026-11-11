using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/enrollments</c>.</summary>
public class EnrollmentRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; init; }

    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }
}
