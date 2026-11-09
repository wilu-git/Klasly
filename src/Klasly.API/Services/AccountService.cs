using System.ComponentModel.DataAnnotations;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Klasly.API.Services;

/// <summary>
/// Account rules: required profile fields per role, unique email / student number / employee no.,
/// hashed passwords, soft delete only, no self-deactivation, guardian links for Parent accounts.
/// </summary>
public class AccountService(IUserRepository users, IPasswordHasher<User> hasher) : IAccountService
{
    public async Task<List<AccountResponse>> ListAsync(UserRole? role, CancellationToken ct) =>
        (await users.ListAsync(role, ct)).Select(ToResponse).ToList();

    public async Task<AccountResponse> GetAsync(int userId, CancellationToken ct) =>
        ToResponse(await FindAsync(userId, ct));

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct)
    {
        var role = request.Role!.Value;
        ValidateProfile(role, request);
        await EnsureUniqueAsync(role, request, null, null, null, ct);

        // CreatedAt is left to the database default (SYSUTCDATETIME()).
        var user = new User { FullName = request.FullName, Email = request.Email, Role = role, IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        switch (role)
        {
            case UserRole.Student: user.Student = new Student(); break;
            case UserRole.Teacher: user.Teacher = new Teacher(); break;
            case UserRole.Parent: user.Guardian = new Guardian(); break;
        }
        ApplyProfile(user, request);

        await users.AddAsync(user, ct); // user + profile in one SaveChanges: no half-created account
        return ToResponse(user);
    }

    public async Task<AccountResponse> UpdateAsync(
        int userId, UpdateAccountRequest request, int currentUserId, CancellationToken ct)
    {
        var user = await FindAsync(userId, ct);
        if (request.IsActive == false) EnsureNotSelf(userId, currentUserId);
        ValidateProfile(user.Role, request);
        await EnsureUniqueAsync(user.Role, request, userId, user.Student?.StudentId, user.Teacher?.TeacherId, ct);

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.IsActive = request.IsActive!.Value;
        user.UpdatedAt = DateTime.UtcNow;
        if (request.NewPassword is not null)
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        ApplyProfile(user, request);

        await users.SaveChangesAsync(ct);
        return ToResponse(user);
    }

    public async Task DeactivateAsync(int userId, int currentUserId, CancellationToken ct)
    {
        var user = await FindAsync(userId, ct);
        EnsureNotSelf(userId, currentUserId);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await users.SaveChangesAsync(ct);
    }

    public async Task<List<LinkedStudentResponse>> ListLinkedStudentsAsync(int parentUserId, CancellationToken ct)
    {
        var guardian = await FindGuardianAsync(parentUserId, ct);
        return (await users.GetLinksAsync(guardian.GuardianId, ct))
            .Select(l => ToResponse(l, l.Student))
            .ToList();
    }

    public async Task<LinkedStudentResponse> LinkStudentAsync(
        int parentUserId, LinkStudentRequest request, CancellationToken ct)
    {
        var guardian = await FindGuardianAsync(parentUserId, ct);
        var student = await users.GetStudentAsync(request.StudentId, ct)
            ?? throw new ValidationException($"Student {request.StudentId} does not exist.");
        if (await users.GetLinkAsync(student.StudentId, guardian.GuardianId, ct) is not null)
            throw new ConflictException($"Student {student.StudentNumber} is already linked to this parent.");

        var link = new StudentGuardian
        {
            StudentId = student.StudentId,
            GuardianId = guardian.GuardianId,
            Relationship = request.Relationship,
            ReceivesAlerts = request.ReceivesAlerts
        };
        await users.AddLinkAsync(link, ct);
        return ToResponse(link, student);
    }

    public async Task UnlinkStudentAsync(int parentUserId, int studentId, CancellationToken ct)
    {
        var guardian = await FindGuardianAsync(parentUserId, ct);
        var link = await users.GetLinkAsync(studentId, guardian.GuardianId, ct)
            ?? throw new KeyNotFoundException($"Student {studentId} is not linked to this parent.");
        await users.RemoveLinkAsync(link, ct);
    }

    private async Task<User> FindAsync(int userId, CancellationToken ct) =>
        await users.GetByIdAsync(userId, ct)
        ?? throw new KeyNotFoundException($"Account {userId} was not found.");

    private async Task<Guardian> FindGuardianAsync(int parentUserId, CancellationToken ct) =>
        (await FindAsync(parentUserId, ct)).Guardian
        ?? throw new KeyNotFoundException($"Account {parentUserId} is not a Parent account.");

    private static void EnsureNotSelf(int userId, int currentUserId)
    {
        if (userId == currentUserId)
            throw new ValidationException("You cannot deactivate your own account.");
    }

    private static void ValidateProfile(UserRole role, AccountRequestBase r)
    {
        var missing = role switch
        {
            UserRole.Teacher when string.IsNullOrWhiteSpace(r.EmployeeNo) => "Employee number",
            UserRole.Student when string.IsNullOrWhiteSpace(r.StudentNumber) => "Student number",
            UserRole.Student when r.YearLevel is null => "Year level",
            UserRole.Student when string.IsNullOrWhiteSpace(r.Program) => "Program",
            _ => null
        };
        if (missing is not null)
            throw new ValidationException($"{missing} is required for a {role} account.");
    }

    private async Task EnsureUniqueAsync(UserRole role, AccountRequestBase r,
        int? excludeUserId, int? excludeStudentId, int? excludeTeacherId, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(r.Email, excludeUserId, ct))
            throw new ConflictException($"Email '{r.Email}' is already used.");
        if (role == UserRole.Student && await users.StudentNumberExistsAsync(r.StudentNumber!, excludeStudentId, ct))
            throw new ConflictException($"Student number '{r.StudentNumber}' is already used.");
        if (role == UserRole.Teacher && await users.EmployeeNoExistsAsync(r.EmployeeNo!, excludeTeacherId, ct))
            throw new ConflictException($"Employee number '{r.EmployeeNo}' is already used.");
    }

    // Copies only the profile fields that belong to the account's role; the rest are ignored.
    private static void ApplyProfile(User user, AccountRequestBase r)
    {
        if (user.Student is { } s)
        {
            s.StudentNumber = r.StudentNumber!;
            s.YearLevel = r.YearLevel!.Value;
            s.Program = r.Program!;
        }
        if (user.Teacher is { } t)
        {
            t.EmployeeNo = r.EmployeeNo!;
            t.Department = r.Department;
        }
        if (user.Guardian is { } g)
        {
            g.ContactEmail = r.ContactEmail ?? r.Email; // alerts go to the login email unless told otherwise
            g.ContactNumber = r.ContactNumber;
        }
    }

    // shortcut: values read from SQL come back as DateTimeKind.Unspecified; stamped UTC here so JSON ends in "Z".
    // Upgrade to a global UTC value converter in KlaslyDbContext once more responses carry dates.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static AccountResponse ToResponse(User u) => new(
        u.UserId, u.FullName, u.Email, u.Role, u.IsActive,
        Utc(u.CreatedAt), u.UpdatedAt is { } updated ? Utc(updated) : null,
        u.Student?.StudentId, u.Student?.StudentNumber, u.Student?.YearLevel, u.Student?.Program,
        u.Teacher?.TeacherId, u.Teacher?.EmployeeNo, u.Teacher?.Department,
        u.Guardian?.GuardianId, u.Guardian?.ContactEmail, u.Guardian?.ContactNumber);

    private static LinkedStudentResponse ToResponse(StudentGuardian link, Student student) => new(
        student.StudentId, student.StudentNumber, student.User.FullName, link.Relationship, link.ReceivesAlerts);
}
