using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<List<SubjectResponse>> ListAsync(CancellationToken ct);
    Task<SubjectResponse> GetAsync(int id, CancellationToken ct);
    Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct);
    Task<SubjectResponse> UpdateAsync(int id, SubjectRequest request, CancellationToken ct);
}
