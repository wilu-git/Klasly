using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.ExcuseRequests — 1-to-0..1 with <see cref="AttendanceRecord"/>.</summary>
public class ExcuseRequest
{
    public int ExcuseId { get; set; }
    public int AttendanceId { get; set; }
    public string Reason { get; set; } = null!;
    public string? AttachmentPath { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Pending;
    public int? ReviewedBy { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? ReviewedAt { get; set; }

    public AttendanceRecord Attendance { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
