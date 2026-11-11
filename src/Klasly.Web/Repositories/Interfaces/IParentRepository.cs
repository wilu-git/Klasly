using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>The parent's API calls: their linked children and one child's overview.</summary>
public interface IParentRepository
{
    Task<List<LinkedStudentResponse>> ListChildrenAsync(CancellationToken ct);
    Task<StudentOverviewResponse> GetOverviewAsync(int studentId, CancellationToken ct);
}
