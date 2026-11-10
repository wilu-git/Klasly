using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for login accounts, their role profiles and guardian links. Business rules stay in the services.</summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<List<User>> ListAsync(UserRole? role, CancellationToken ct);

    /// <summary>Tracked, with Student / Teacher / Guardian loaded.</summary>
    Task<User?> GetByIdAsync(int userId, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, int? excludeUserId, CancellationToken ct);
    Task<bool> StudentNumberExistsAsync(string studentNumber, int? excludeStudentId, CancellationToken ct);
    Task<bool> EmployeeNoExistsAsync(string employeeNo, int? excludeTeacherId, CancellationToken ct);

    /// <summary>Inserts the user together with any profile set on its navigation properties.</summary>
    /// <summary>
    /// Saves this phone's FCM token on the user and clears it from anyone else who had it (a shared phone), in one transaction.
    /// </summary>
    Task SetFcmTokenAsync(int userId, string token, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Teacher with its User, or null.</summary>
    Task<Teacher?> GetTeacherAsync(int teacherId, CancellationToken ct);

    /// <summary>Student with its User, or null.</summary>
    Task<Student?> GetStudentAsync(int studentId, CancellationToken ct);

    Task<List<StudentGuardian>> GetLinksAsync(int guardianId, CancellationToken ct);
    Task<StudentGuardian?> GetLinkAsync(int studentId, int guardianId, CancellationToken ct);
    Task AddLinkAsync(StudentGuardian link, CancellationToken ct);

    /// <summary>The one hard delete in Klasly (approved): removes a wrong guardian link.</summary>
    Task RemoveLinkAsync(StudentGuardian link, CancellationToken ct);
}
