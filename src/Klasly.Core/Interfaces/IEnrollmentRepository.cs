using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for enrollments. Dropping sets Status = Dropped; rows are never removed.</summary>
public interface IEnrollmentRepository
{
    /// <summary>Active and Dropped enrollments of a class, with Student.User loaded, by student name.</summary>
    Task<List<Enrollment>> ListByClassAsync(int classId, CancellationToken ct);

    /// <summary>Tracked.</summary>
    Task<Enrollment?> GetByIdAsync(int enrollmentId, CancellationToken ct);

    /// <summary>Tracked. (ClassId, StudentId) is unique, so there is at most one row.</summary>
    Task<Enrollment?> GetByClassAndStudentAsync(int classId, int studentId, CancellationToken ct);

    Task AddAsync(Enrollment enrollment, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
