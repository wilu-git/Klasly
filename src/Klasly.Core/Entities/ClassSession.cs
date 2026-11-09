namespace Klasly.Core.Entities;

/// <summary>One class meeting and its QR check-in window. All times are UTC.</summary>
public class ClassSession
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int LateAfterMinutes { get; set; } = 15;
    public string QrNonce { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public DateTime? ClosedAt { get; set; }

    public Class Class { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
