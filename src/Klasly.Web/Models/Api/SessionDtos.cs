namespace Klasly.Web.Models.Api;

/// <summary>Body of <c>POST /api/classes/{classId}/sessions</c>. Times carry their offset (+08:00 for Manila).</summary>
public record CreateSessionRequest(DateTimeOffset StartTime, DateTimeOffset EndTime, int LateAfterMinutes);

/// <summary>A class session. Times are UTC. <see cref="Status"/>: Scheduled, Open, Closed, Finalized or Cancelled.</summary>
public record SessionResponse(
    int SessionId, int ClassId, string SubjectCode, string Section,
    DateTime StartTimeUtc, DateTime EndTimeUtc, int LateAfterMinutes, string Status, DateTime? ClosedAtUtc)
{
    /// <summary>The API treats an Open session past its end time as closed (no QR, no scans).</summary>
    public bool IsLive => Status == "Open" && EndTimeUtc > DateTime.UtcNow;

    public string DisplayStatus => Status == "Open" && !IsLive ? "Ended" : Status;
}

/// <summary>The QR token to show now; it rotates every <see cref="StepSeconds"/>.</summary>
public record QrTokenResponse(string Token, DateTime ExpiresAtUtc, int StepSeconds);

/// <summary>One actively enrolled student on the roster. <see cref="Status"/> null = not scanned yet.</summary>
public record SessionAttendanceRow(
    int StudentId, string StudentNumber, string FullName, string? Status, DateTime? ScannedAtUtc, string? Source);
