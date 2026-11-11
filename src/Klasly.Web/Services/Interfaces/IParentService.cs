using Klasly.Web.Models.Api;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services.Interfaces;

public interface IParentService
{
    Task<List<LinkedStudentResponse>> GetChildrenAsync(CancellationToken ct);

    /// <summary>One child's overview and the sibling list. The API answers 403 for a child who isn't linked.</summary>
    Task<ChildOverviewViewModel> GetChildAsync(int studentId, CancellationToken ct);
}
