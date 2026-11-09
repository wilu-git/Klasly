namespace Klasly.Core.Entities;

/// <summary>One attendance result per student per session. All times are UTC.</summary>
public class AttendanceRecord
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime? ScannedAt { get; set; }
    public DateTime MarkedAt { get; set; }
    public AttendanceSource Source { get; set; }

    /// <summary>UserId of the teacher who corrected the record; null for scans and Worker rows.</summary>
    public int? MarkedBy { get; set; }

    public ClassSession Session { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public User? MarkedByUser { get; set; }
    public ExcuseRequest? ExcuseRequest { get; set; }
}
