using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

/// <summary>Shapes the Parent pages from the API. No rules here: the API decides who sees which child.</summary>
public class ParentService(IParentRepository parents) : IParentService
{
    public Task<List<LinkedStudentResponse>> GetChildrenAsync(CancellationToken ct) => parents.ListChildrenAsync(ct);

    public async Task<ChildOverviewViewModel> GetChildAsync(int studentId, CancellationToken ct)
    {
        // Overview first: an unlinked id fails with the API's 403 before the second call.
        var overview = await parents.GetOverviewAsync(studentId, ct);
        return new ChildOverviewViewModel(overview, await parents.ListChildrenAsync(ct));
    }
}
