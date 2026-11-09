using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface IClassService
{
    Task<List<ClassResponse>> ListAsync(int? termId, CancellationToken ct);
    Task<ClassResponse> GetAsync(int classId, CancellationToken ct);
    Task<ClassResponse> CreateAsync(ClassRequest request, CancellationToken ct);
    Task<ClassResponse> UpdateAsync(int classId, ClassRequest request, CancellationToken ct);
}
