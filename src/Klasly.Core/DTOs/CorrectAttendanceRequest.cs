using System.ComponentModel.DataAnnotations;
using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>PUT /api/sessions/{id}/attendance/{studentId}</c>: the teacher's corrected status.</summary>
public class CorrectAttendanceRequest
{
    [Required]
    public AttendanceStatus? Status { get; init; }
}
