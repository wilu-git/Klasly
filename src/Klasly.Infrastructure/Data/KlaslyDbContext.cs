using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Data;

/// <summary>
/// Maps the 17 tables documented in docs/Klasly_Schema.sql.
/// Every datetime2 column stores UTC; convert to Asia/Manila only for display.
/// </summary>
public class KlaslyDbContext : DbContext
{
    public KlaslyDbContext(DbContextOptions<KlaslyDbContext> options)
        : base(options)
    {
    }

    // 1. Accounts and people
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

    // 2. Academic structure
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassOffering> Classes => Set<ClassOffering>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    // 3. Attendance
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<ExcuseRequest> ExcuseRequests => Set<ExcuseRequest>();

    // 4. Grades and risk
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<RiskFlag> RiskFlags => Set<RiskFlag>();

    // 5. Notifications and settings
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KlaslyDbContext).Assembly);

        // Section 6 of the schema — default settings. No secrets here;
        // API keys stay in user-secrets or environment variables.
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Key = "AbsenceThreshold", Value = "0.20" },
            new SystemSetting { Key = "PassingGrade", Value = "75" },
            new SystemSetting { Key = "QrStepSeconds", Value = "30" },
            new SystemSetting { Key = "MaxEmailAttempts", Value = "5" },
            new SystemSetting { Key = "TimeZoneId", Value = "Asia/Manila" });
    }
}
