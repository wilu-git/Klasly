using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface ISessionService
{
    Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct);
    Task<TeacherClassViewModel> GetClassAsync(int classId, CancellationToken ct);
}
