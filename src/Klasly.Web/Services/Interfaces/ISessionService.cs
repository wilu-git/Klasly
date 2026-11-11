using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface ISessionService
{
    Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct);
    Task<TeacherClassViewModel> GetClassAsync(int classId, CancellationToken ct);

    /// <summary>Creates the session (starting now) and opens it. OpenError is set when only the create worked.</summary>
    Task<(int SessionId, string? OpenError)> StartAsync(StartSessionViewModel model, CancellationToken ct);
    Task<SessionPageViewModel> GetSessionAsync(int sessionId, CancellationToken ct);

    /// <summary>The QR (null when closed or past its end) and the roster, for the 5-second poll.</summary>
    Task<SessionLiveViewModel> GetLiveAsync(int sessionId, CancellationToken ct);
    /// <summary>The teacher's correction after the session ended; returns the updated row.</summary>
    Task<SessionAttendanceRow> CorrectAttendanceAsync(int sessionId, int studentId, string? status, CancellationToken ct);
    Task OpenAsync(int sessionId, CancellationToken ct);
    Task CloseAsync(int sessionId, CancellationToken ct);
}
