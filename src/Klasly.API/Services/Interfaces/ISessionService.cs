using Klasly.Core.DTOs;
using Klasly.Core.Entities;

namespace Klasly.API.Services.Interfaces;

/// <summary>Teacher-side session management. Every method checks that the caller owns the class.</summary>
public interface ISessionService
{
    Task<List<SessionResponse>> ListAsync(int classId, int userId, CancellationToken ct);
    Task<SessionResponse> CreateAsync(int classId, CreateSessionRequest request, int userId, CancellationToken ct);
    Task<SessionResponse> GetAsync(int sessionId, int userId, CancellationToken ct);
    Task<SessionResponse> OpenAsync(int sessionId, int userId, CancellationToken ct);
    Task<SessionResponse> CloseAsync(int sessionId, int userId, CancellationToken ct);

    /// <summary>The current QR token; only while the session is open and not past its end time.</summary>
    Task<QrTokenResponse> GetQrAsync(int sessionId, int userId, CancellationToken ct);

    /// <summary>Every actively enrolled student with their result so far, by name.</summary>
    Task<List<SessionAttendanceRow>> GetAttendanceAsync(int sessionId, int userId, CancellationToken ct);

    /// <summary>The teacher's correction of one student's result, only after the session has ended (Closed or Finalized).</summary>
    Task<SessionAttendanceRow> CorrectAttendanceAsync(
        int sessionId, int studentId, AttendanceStatus status, int userId, CancellationToken ct);
}
