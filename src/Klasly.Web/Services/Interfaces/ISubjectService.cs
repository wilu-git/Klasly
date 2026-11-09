using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectResponse>> ListAsync(CancellationToken ct);
    Task<SubjectFormViewModel> GetFormAsync(int id, CancellationToken ct);
    Task CreateAsync(SubjectFormViewModel model, CancellationToken ct);
    Task UpdateAsync(SubjectFormViewModel model, CancellationToken ct);
}
