using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class UserRepository(KlaslyDbContext context) : IUserRepository
{
    // Email is unique (UQ_Users_Email); the default SQL Server collation makes the match case-insensitive.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<List<User>> ListAsync(UserRole? role, CancellationToken ct) =>
        WithProfiles(context.Users.AsNoTracking())
            .Where(u => role == null || u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public Task<User?> GetByIdAsync(int userId, CancellationToken ct) =>
        WithProfiles(context.Users).FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId, CancellationToken ct) =>
        context.Users.AnyAsync(u => u.Email == email && u.UserId != excludeUserId, ct);

    public Task<bool> StudentNumberExistsAsync(string studentNumber, int? excludeStudentId, CancellationToken ct) =>
        context.Students.AnyAsync(s => s.StudentNumber == studentNumber && s.StudentId != excludeStudentId, ct);

    public Task<bool> EmployeeNoExistsAsync(string employeeNo, int? excludeTeacherId, CancellationToken ct) =>
        context.Teachers.AnyAsync(t => t.EmployeeNo == employeeNo && t.TeacherId != excludeTeacherId, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        context.Users.Add(user); // profile rows come along through the navigation properties, same transaction
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    public Task<Teacher?> GetTeacherAsync(int teacherId, CancellationToken ct) =>
        context.Teachers.AsNoTracking().Include(t => t.User).FirstOrDefaultAsync(t => t.TeacherId == teacherId, ct);

    public Task<Student?> GetStudentAsync(int studentId, CancellationToken ct) =>
        context.Students.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == studentId, ct);

    public Task<List<StudentGuardian>> GetLinksAsync(int guardianId, CancellationToken ct) =>
        context.StudentGuardians.AsNoTracking()
            .Include(l => l.Student).ThenInclude(s => s.User)
            .Where(l => l.GuardianId == guardianId)
            .OrderBy(l => l.Student.User.FullName)
            .ToListAsync(ct);

    public Task<StudentGuardian?> GetLinkAsync(int studentId, int guardianId, CancellationToken ct) =>
        context.StudentGuardians.FirstOrDefaultAsync(l => l.StudentId == studentId && l.GuardianId == guardianId, ct);

    public async Task AddLinkAsync(StudentGuardian link, CancellationToken ct)
    {
        context.StudentGuardians.Add(link);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveLinkAsync(StudentGuardian link, CancellationToken ct)
    {
        context.StudentGuardians.Remove(link);
        await context.SaveChangesAsync(ct);
    }

    private static IQueryable<User> WithProfiles(IQueryable<User> users) =>
        users.Include(u => u.Student).Include(u => u.Teacher).Include(u => u.Guardian);
}
