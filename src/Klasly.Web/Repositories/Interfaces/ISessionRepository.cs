using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>The teacher's API calls: own classes, and (from Task 2 on) their sessions, QR and roster.</summary>
public interface ISessionRepository
{
    Task<List<ClassResponse>> ListMyClassesAsync(CancellationToken ct);
}
