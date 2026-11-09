using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface IClassService
{
    Task<ClassListViewModel> GetListAsync(int? termId, CancellationToken ct);
    Task<ClassFormViewModel> GetNewFormAsync(CancellationToken ct);
    Task<ClassFormViewModel> GetEditFormAsync(int id, CancellationToken ct);
    Task FillOptionsAsync(ClassFormViewModel model, CancellationToken ct);
    Task CreateAsync(ClassFormViewModel model, CancellationToken ct);
    Task UpdateAsync(ClassFormViewModel model, CancellationToken ct);

    Task<ClassDetailsViewModel> GetDetailsAsync(int id, CancellationToken ct);
    Task EnrollAsync(int classId, int studentId, CancellationToken ct);
    Task DropAsync(int enrollmentId, CancellationToken ct);
}
