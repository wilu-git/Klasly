using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

/// <summary>Shapes the Teacher pages from the session API calls. No business rules here (the API decides).</summary>
public class SessionService(ISessionRepository sessions) : ISessionService
{
    public Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct) => sessions.ListMyClassesAsync(ct);

    public async Task<TeacherClassViewModel> GetClassAsync(int classId, CancellationToken ct)
    {
        // Sessions first: the API answers 403/404 for a class that isn't this teacher's, before we look for the header.
        var list = await sessions.ListByClassAsync(classId, ct);
        var header = (await sessions.ListMyClassesAsync(ct)).FirstOrDefault(c => c.ClassId == classId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Class not found.", new Dictionary<string, string[]>());
        return new TeacherClassViewModel(header, list);
    }
}
