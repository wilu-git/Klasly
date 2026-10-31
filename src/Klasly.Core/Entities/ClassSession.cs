using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.ClassSessions — a single meeting of a class offering.</summary>
public class ClassSession
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }

    /// <summary>UTC.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>UTC.</summary>
    public DateTime EndTime { get; set; }

    public int LateAfterMinutes { get; set; } = 15;

    /// <summary>Per-session random value; the HMAC key itself lives in app config.</summary>
    public string QrNonce { get; set; } = null!;

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    /// <summary>UTC.</summary>
    public DateTime? ClosedAt { get; set; }

    public ClassOffering Class { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
