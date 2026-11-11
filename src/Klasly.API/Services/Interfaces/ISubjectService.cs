using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectResponse>> ListAsync(CancellationToken ct);
    Task<SubjectResponse> GetAsync(int subjectId, CancellationToken ct);
    Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct);
    Task<SubjectResponse> UpdateAsync(int subjectId, SubjectRequest request, CancellationToken ct);
}
