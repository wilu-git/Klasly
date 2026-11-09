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
    Task OpenAsync(int sessionId, CancellationToken ct);
    Task CloseAsync(int sessionId, CancellationToken ct);
}
