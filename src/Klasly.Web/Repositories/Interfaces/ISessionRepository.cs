using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>The teacher's API calls: own classes, their sessions, the QR token and the live roster.</summary>
public interface ISessionRepository
{
    Task<List<ClassResponse>> ListMyClassesAsync(CancellationToken ct);
    Task<List<SessionResponse>> ListByClassAsync(int classId, CancellationToken ct);
    Task<SessionResponse> CreateAsync(int classId, CreateSessionRequest request, CancellationToken ct);
    Task<SessionResponse> GetAsync(int sessionId, CancellationToken ct);
    Task<SessionResponse> OpenAsync(int sessionId, CancellationToken ct);
    Task<SessionResponse> CloseAsync(int sessionId, CancellationToken ct);
    Task<QrTokenResponse> GetQrAsync(int sessionId, CancellationToken ct);
    Task<List<SessionAttendanceRow>> GetAttendanceAsync(int sessionId, CancellationToken ct);
    Task<SessionAttendanceRow> CorrectAttendanceAsync(int sessionId, int studentId, CorrectAttendanceRequest request, CancellationToken ct);
}
