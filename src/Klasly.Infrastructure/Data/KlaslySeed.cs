using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Data;

/// <summary>
/// Model seed data: the five SystemSettings rows plus a small set of fictional test accounts and classes.
/// Every value is a constant, because HasData is baked into the migration.
/// No real person appears here (Data Privacy Act of 2012, RA 10173).
/// </summary>
internal static class KlaslySeed
{
    /// <summary>Stamp used for every seeded CreatedAt/UpdatedAt so migrations stay deterministic.</summary>
    private static readonly DateTime SeededAt = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// ASP.NET Core Identity (PBKDF2-HMAC-SHA512, 100k iterations) hash of the shared test password
    /// "Klasly@123". Test data only — fine for a fixed salt, never for a real account.
    /// </summary>
    private const string TestPasswordHash =
        "AQAAAAIAAYagAAAAEEtsYXNseVNlZWRTYWx0MDEX1GgZ4AUyzBRe3YPgwwRqqYTB9ezchNLeCJcAssAQfw==";

    public static void SeedKlasly(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Key = "AbsenceThreshold", Value = "0.20", UpdatedAt = SeededAt },
            new SystemSetting { Key = "PassingGrade", Value = "75", UpdatedAt = SeededAt },
            new SystemSetting { Key = "QrStepSeconds", Value = "30", UpdatedAt = SeededAt },
            new SystemSetting { Key = "MaxEmailAttempts", Value = "5", UpdatedAt = SeededAt },
            new SystemSetting { Key = "TimeZoneId", Value = "Asia/Manila", UpdatedAt = SeededAt });

        modelBuilder.Entity<User>().HasData(
            NewUser(1, "Klasly Administrator", "admin@klasly.test", UserRole.Admin),
            NewUser(2, "Rosa Villanueva", "rosa.villanueva@klasly.test", UserRole.Teacher),
            NewUser(3, "Mark Delos Santos", "mark.delossantos@klasly.test", UserRole.Teacher),
            NewUser(4, "Aira Mendoza", "aira.mendoza@klasly.test", UserRole.Student),
            NewUser(5, "Bryan Lacsamana", "bryan.lacsamana@klasly.test", UserRole.Student),
            NewUser(6, "Carla Dimaano", "carla.dimaano@klasly.test", UserRole.Student),
            NewUser(7, "Dennis Orpilla", "dennis.orpilla@klasly.test", UserRole.Student),
            NewUser(8, "Ellen Bautista", "ellen.bautista@klasly.test", UserRole.Student),
            NewUser(9, "Francis Nuqui", "francis.nuqui@klasly.test", UserRole.Student),
            NewUser(10, "Marites Mendoza", "marites.mendoza@klasly.test", UserRole.Parent),
            NewUser(11, "Rodel Dimaano", "rodel.dimaano@klasly.test", UserRole.Parent),
            NewUser(12, "Linda Bautista", "linda.bautista@klasly.test", UserRole.Parent));

        modelBuilder.Entity<Teacher>().HasData(
            new Teacher { TeacherId = 1, UserId = 2, EmployeeNo = "EMP-1001", Department = "College of Computing" },
            new Teacher { TeacherId = 2, UserId = 3, EmployeeNo = "EMP-1002", Department = "College of Computing" });

        modelBuilder.Entity<Student>().HasData(
            new Student { StudentId = 1, UserId = 4, StudentNumber = "2026-00001", YearLevel = 3, Program = "BSCpE" },
            new Student { StudentId = 2, UserId = 5, StudentNumber = "2026-00002", YearLevel = 3, Program = "BSCpE" },
            new Student { StudentId = 3, UserId = 6, StudentNumber = "2026-00003", YearLevel = 3, Program = "BSCpE" },
            new Student { StudentId = 4, UserId = 7, StudentNumber = "2026-00004", YearLevel = 3, Program = "BSCpE" },
            new Student { StudentId = 5, UserId = 8, StudentNumber = "2026-00005", YearLevel = 3, Program = "BSCpE" },
            new Student { StudentId = 6, UserId = 9, StudentNumber = "2026-00006", YearLevel = 3, Program = "BSCpE" });

        modelBuilder.Entity<Guardian>().HasData(
            new Guardian { GuardianId = 1, UserId = 10, ContactEmail = "marites.mendoza@klasly.test", ContactNumber = "+639170000001" },
            new Guardian { GuardianId = 2, UserId = 11, ContactEmail = "rodel.dimaano@klasly.test", ContactNumber = "+639170000002" },
            new Guardian { GuardianId = 3, UserId = 12, ContactEmail = "linda.bautista@klasly.test", ContactNumber = "+639170000003" });

        modelBuilder.Entity<StudentGuardian>().HasData(
            new StudentGuardian { StudentId = 1, GuardianId = 1, Relationship = "Mother", ReceivesAlerts = true },
            new StudentGuardian { StudentId = 2, GuardianId = 1, Relationship = "Mother", ReceivesAlerts = true },
            new StudentGuardian { StudentId = 3, GuardianId = 2, Relationship = "Father", ReceivesAlerts = true },
            new StudentGuardian { StudentId = 4, GuardianId = 2, Relationship = "Father", ReceivesAlerts = true },
            new StudentGuardian { StudentId = 5, GuardianId = 3, Relationship = "Guardian", ReceivesAlerts = true },
            new StudentGuardian { StudentId = 6, GuardianId = 3, Relationship = "Guardian", ReceivesAlerts = true });

        modelBuilder.Entity<AcademicTerm>().HasData(
            new AcademicTerm
            {
                TermId = 1,
                Name = "1st Sem 2026-27",
                StartDate = new DateOnly(2026, 8, 17),
                EndDate = new DateOnly(2026, 12, 19),
                IsActive = true
            });

        modelBuilder.Entity<Subject>().HasData(
            new Subject { SubjectId = 1, Code = "ELECT3", Title = "Mobile Application Development", Units = 3 },
            new Subject { SubjectId = 2, Code = "IT302", Title = "Software Engineering", Units = 3 });

        modelBuilder.Entity<Class>().HasData(
            new Class
            {
                ClassId = 1, SubjectId = 1, TeacherId = 1, TermId = 1,
                Section = "BSCpE 3-A", Schedule = "MWF 07:30-09:00", Room = "CL-301"
            },
            new Class
            {
                ClassId = 2, SubjectId = 2, TeacherId = 2, TermId = 1,
                Section = "BSCpE 3-A", Schedule = "TTh 13:00-14:30", Room = "CL-205"
            });

        // All six students take ELECT3; the first three also take IT302.
        modelBuilder.Entity<Enrollment>().HasData(
            NewEnrollment(1, 1, 1), NewEnrollment(2, 1, 2), NewEnrollment(3, 1, 3),
            NewEnrollment(4, 1, 4), NewEnrollment(5, 1, 5), NewEnrollment(6, 1, 6),
            NewEnrollment(7, 2, 1), NewEnrollment(8, 2, 2), NewEnrollment(9, 2, 3));
    }

    private static User NewUser(int userId, string fullName, string email, UserRole role) => new()
    {
        UserId = userId,
        FullName = fullName,
        Email = email,
        PasswordHash = TestPasswordHash,
        Role = role,
        IsActive = true,
        CreatedAt = SeededAt
    };

    private static Enrollment NewEnrollment(int enrollmentId, int classId, int studentId) => new()
    {
        EnrollmentId = enrollmentId,
        ClassId = classId,
        StudentId = studentId,
        Status = EnrollmentStatus.Active
    };
}
