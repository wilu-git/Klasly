using System.ComponentModel.DataAnnotations;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>Enrollment rules: class and active student must exist; one row per class + student; drop = soft delete.</summary>
public class EnrollmentService(
    IEnrollmentRepository enrollments, IClassRepository classes, IUserRepository users) : IEnrollmentService
{
    public async Task<List<EnrollmentResponse>> ListByClassAsync(int classId, CancellationToken ct)
    {
        if (await classes.GetByIdAsync(classId, ct) is null)
            throw new KeyNotFoundException($"Class {classId} was not found.");
        return (await enrollments.ListByClassAsync(classId, ct)).Select(e => ToResponse(e, e.Student)).ToList();
    }

    public async Task<(EnrollmentResponse Enrollment, bool Created)> EnrollAsync(
        EnrollmentRequest request, CancellationToken ct)
    {
        if (await classes.GetByIdAsync(request.ClassId, ct) is null)
            throw new ValidationException($"Class {request.ClassId} does not exist.");
        var student = await users.GetStudentAsync(request.StudentId, ct)
            ?? throw new ValidationException($"Student {request.StudentId} does not exist.");
        if (!student.User.IsActive)
            throw new ValidationException($"Student {student.StudentNumber}'s account is inactive.");

        // (ClassId, StudentId) is unique, so a dropped student is re-enrolled by reusing their old row.
        var existing = await enrollments.GetByClassAndStudentAsync(request.ClassId, request.StudentId, ct);
        if (existing is { Status: EnrollmentStatus.Active })
            throw new ConflictException($"Student {student.StudentNumber} is already enrolled in this class.");
        if (existing is not null)
        {
            existing.Status = EnrollmentStatus.Active;
            await enrollments.SaveChangesAsync(ct);
            return (ToResponse(existing, student), false);
        }

        var enrollment = new Enrollment
        {
            ClassId = request.ClassId,
            StudentId = request.StudentId,
            Status = EnrollmentStatus.Active
        };
        await enrollments.AddAsync(enrollment, ct);
        return (ToResponse(enrollment, student), true);
    }

    public async Task DropAsync(int enrollmentId, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} was not found.");
        enrollment.Status = EnrollmentStatus.Dropped;
        await enrollments.SaveChangesAsync(ct);
    }

    private static EnrollmentResponse ToResponse(Enrollment e, Student s) =>
        new(e.EnrollmentId, e.ClassId, e.StudentId, s.StudentNumber, s.User.FullName, e.Status);
}
