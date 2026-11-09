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
}
