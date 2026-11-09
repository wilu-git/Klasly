using Klasly.Web.Models.Api;

namespace Klasly.Web.Services.Interfaces;

public interface ISessionService
{
    Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct);
}
