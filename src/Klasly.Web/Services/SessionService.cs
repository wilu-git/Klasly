using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;

namespace Klasly.Web.Services;

/// <summary>Shapes the Teacher pages from the session API calls. No business rules here (the API decides).</summary>
public class SessionService(ISessionRepository sessions) : ISessionService
{
    public Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct) => sessions.ListMyClassesAsync(ct);
}
