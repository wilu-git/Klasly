using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface IEnrollmentService
{
    Task<List<EnrollmentResponse>> ListByClassAsync(int classId, CancellationToken ct);

    /// <returns>The enrollment, and <c>Created = false</c> when a Dropped row was reactivated instead.</returns>
    Task<(EnrollmentResponse Enrollment, bool Created)> EnrollAsync(EnrollmentRequest request, CancellationToken ct);

    Task DropAsync(int enrollmentId, CancellationToken ct);
}
