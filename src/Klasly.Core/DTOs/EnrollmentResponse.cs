using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>A student's seat in a class. <see cref="Status"/> is Active or Dropped.</summary>
public record EnrollmentResponse(
    int EnrollmentId, int ClassId, int StudentId, string StudentNumber, string StudentName, EnrollmentStatus Status);
