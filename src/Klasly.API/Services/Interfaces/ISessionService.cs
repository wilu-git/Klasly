using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

/// <summary>Teacher-side session management. Every method checks that the caller owns the class.</summary>
public interface ISessionService
{
    Task<List<SessionResponse>> ListAsync(int classId, int userId, CancellationToken ct);
    Task<SessionResponse> CreateAsync(int classId, CreateSessionRequest request, int userId, CancellationToken ct);
    Task<SessionResponse> GetAsync(int sessionId, int userId, CancellationToken ct);
}
