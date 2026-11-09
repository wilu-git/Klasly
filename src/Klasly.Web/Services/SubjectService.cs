using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

public class SubjectService(ISubjectRepository subjects) : ISubjectService
{
    public Task<List<SubjectResponse>> ListAsync(CancellationToken ct) => subjects.ListAsync(ct);

    public async Task<SubjectFormViewModel> GetFormAsync(int id, CancellationToken ct)
    {
        var subject = await subjects.GetAsync(id, ct);
        return new SubjectFormViewModel { Id = subject.SubjectId, Code = subject.Code, Title = subject.Title, Units = subject.Units };
    }

    public Task CreateAsync(SubjectFormViewModel model, CancellationToken ct) => subjects.CreateAsync(ToRequest(model), ct);

    public Task UpdateAsync(SubjectFormViewModel model, CancellationToken ct) =>
        subjects.UpdateAsync(model.Id!.Value, ToRequest(model), ct);

    private static SubjectRequest ToRequest(SubjectFormViewModel model) => new(model.Code, model.Title, model.Units);
}
