using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class SessionRepository(HttpClient http) : ISessionRepository
{
    // The API sorts it: active term first, then subject code and section.
    public Task<List<ClassResponse>> ListMyClassesAsync(CancellationToken ct) =>
        http.GetApiAsync<List<ClassResponse>>("api/teacher/classes", ct);
}
