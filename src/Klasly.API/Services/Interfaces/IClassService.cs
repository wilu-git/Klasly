using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface IClassService
{
    Task<List<ClassResponse>> ListAsync(int? termId, CancellationToken ct);
    /// <summary>Classes of the teacher behind <paramref name="userId"/>, active term first, then by subject code.</summary>
    Task<List<ClassResponse>> ListForTeacherAsync(int userId, CancellationToken ct);
    Task<ClassResponse> GetAsync(int classId, CancellationToken ct);
    Task<ClassResponse> CreateAsync(ClassRequest request, CancellationToken ct);
    Task<ClassResponse> UpdateAsync(int classId, ClassRequest request, CancellationToken ct);
}
