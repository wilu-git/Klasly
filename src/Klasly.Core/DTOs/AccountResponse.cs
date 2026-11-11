using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>
/// An account with its role profile flattened in; fields for other roles are null.
/// Never carries PasswordHash, FcmToken or RegisteredDeviceId.
/// </summary>
public record AccountResponse(
    int UserId,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int? StudentId,
    string? StudentNumber,
    int? YearLevel,
    string? Program,
    int? TeacherId,
    string? EmployeeNo,
    string? Department,
    int? GuardianId,
    string? ContactEmail,
    string? ContactNumber);
