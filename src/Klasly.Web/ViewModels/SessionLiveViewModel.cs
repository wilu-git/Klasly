using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>
/// The part of the session page that refreshes: the QR (null once the session is closed or past its end) and the roster.
/// <paramref name="CanCorrect"/>: the session is Closed or Finalized, so each row gets a status dropdown. Never true in the poll.
/// </summary>
public record SessionLiveViewModel(int SessionId, string? QrSvg, List<SessionAttendanceRow> Roster, bool CanCorrect = false)
{
    public static readonly string[] Statuses = ["Present", "Late", "Absent", "Excused"];

    public bool IsClosed => QrSvg is null;
    public int Present => Roster.Count(r => r.Status == "Present");
    public int Late => Roster.Count(r => r.Status == "Late");
    public int CheckedIn => Present + Late;
    public string Summary => $"{CheckedIn} of {Roster.Count} checked in · {Present} Present · {Late} Late";
}
