using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.AttendanceRecords — one row per student per session.</summary>
public class AttendanceRecord
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; }

    /// <summary>UTC; NULL when the source is Auto or Manual.</summary>
    public DateTime? ScannedAt { get; set; }

    /// <summary>UTC.</summary>
    public DateTime MarkedAt { get; set; }

    public AttendanceSource Source { get; set; }

    /// <summary>The teacher (Users row) who corrected this record, if any.</summary>
    public int? MarkedBy { get; set; }

    public ClassSession Session { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public User? MarkedByUser { get; set; }
    public ExcuseRequest? ExcuseRequest { get; set; }
}
