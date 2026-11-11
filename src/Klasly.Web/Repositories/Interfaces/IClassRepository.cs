using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>Classes and their enrollments (enrollments are only managed from a class's page).</summary>
public interface IClassRepository
{
    Task<List<ClassResponse>> ListAsync(int? termId, CancellationToken ct);
    Task<ClassResponse> GetAsync(int id, CancellationToken ct);
    Task<ClassResponse> CreateAsync(ClassRequest request, CancellationToken ct);
    Task<ClassResponse> UpdateAsync(int id, ClassRequest request, CancellationToken ct);

    Task<List<EnrollmentResponse>> ListEnrollmentsAsync(int classId, CancellationToken ct);
    Task<EnrollmentResponse> EnrollAsync(EnrollmentRequest request, CancellationToken ct);
    Task DropAsync(int enrollmentId, CancellationToken ct);
}
