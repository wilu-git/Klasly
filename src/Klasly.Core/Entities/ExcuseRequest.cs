namespace Klasly.Core.Entities;

/// <summary>An excuse filed against one attendance record (one-to-zero-or-one).</summary>
public class ExcuseRequest
{
    public int ExcuseId { get; set; }
    public int AttendanceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
    public ExcuseStatus Status { get; set; } = ExcuseStatus.Pending;
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public AttendanceRecord Attendance { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
