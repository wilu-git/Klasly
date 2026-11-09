using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class SessionRepository(HttpClient http) : ISessionRepository
{
    // The API sorts it: active term first, then subject code and section.
    public Task<List<ClassResponse>> ListMyClassesAsync(CancellationToken ct) =>
        http.GetApiAsync<List<ClassResponse>>("api/teacher/classes", ct);

    // Newest first (API order). Another teacher's class → 403.
    public Task<List<SessionResponse>> ListByClassAsync(int classId, CancellationToken ct) =>
        http.GetApiAsync<List<SessionResponse>>($"api/classes/{classId}/sessions", ct);

    // Creates it as Scheduled.
    public Task<SessionResponse> CreateAsync(int classId, CreateSessionRequest request, CancellationToken ct) =>
        http.PostApiAsync<SessionResponse>($"api/classes/{classId}/sessions", request, ct);

    public Task<SessionResponse> GetAsync(int sessionId, CancellationToken ct) =>
        http.GetApiAsync<SessionResponse>($"api/sessions/{sessionId}", ct);

    public Task<SessionResponse> OpenAsync(int sessionId, CancellationToken ct) =>
        http.PostApiAsync<SessionResponse>($"api/sessions/{sessionId}/open", null, ct);

    public Task<SessionResponse> CloseAsync(int sessionId, CancellationToken ct) =>
        http.PostApiAsync<SessionResponse>($"api/sessions/{sessionId}/close", null, ct);
}
