using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>The part of the session page that refreshes: the QR (null once the session is closed or past its end) and the roster.</summary>
public record SessionLiveViewModel(int SessionId, string? QrSvg, List<SessionAttendanceRow> Roster)
{
    public bool IsClosed => QrSvg is null;
    public int Present => Roster.Count(r => r.Status == "Present");
    public int Late => Roster.Count(r => r.Status == "Late");
    public int CheckedIn => Present + Late;
    public string Summary => $"{CheckedIn} of {Roster.Count} checked in · {Present} Present · {Late} Late";
}
