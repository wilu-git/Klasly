using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

// Section 1 of Klasly_Schema.sql — accounts and people.

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
            t.HasCheckConstraint("CK_Users_Role", "Role IN (N'Admin', N'Teacher', N'Student', N'Parent')"));

        builder.HasKey(e => e.UserId).HasName("PK_Users");

        builder.Property(e => e.FullName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.FcmToken).HasMaxLength(500);
        builder.Property(e => e.RegisteredDeviceId).HasMaxLength(200);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
    }
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students", t =>
            t.HasCheckConstraint("CK_Students_YearLevel", "YearLevel BETWEEN 1 AND 6"));

        builder.HasKey(e => e.StudentId).HasName("PK_Students");

        builder.Property(e => e.StudentNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Program).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("UQ_Students_UserId");
        builder.HasIndex(e => e.StudentNumber).IsUnique().HasDatabaseName("UQ_Students_StudentNumber");

        builder.HasOne(e => e.User)
            .WithOne(u => u.Student!)
            .HasForeignKey<Student>(e => e.UserId)
            .HasConstraintName("FK_Students_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");

        builder.HasKey(e => e.TeacherId).HasName("PK_Teachers");

        builder.Property(e => e.EmployeeNo).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100);

        builder.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("UQ_Teachers_UserId");
        builder.HasIndex(e => e.EmployeeNo).IsUnique().HasDatabaseName("UQ_Teachers_EmployeeNo");

        builder.HasOne(e => e.User)
            .WithOne(u => u.Teacher!)
            .HasForeignKey<Teacher>(e => e.UserId)
            .HasConstraintName("FK_Teachers_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians");

        builder.HasKey(e => e.GuardianId).HasName("PK_Guardians");

        builder.Property(e => e.ContactEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ContactNumber).HasMaxLength(20);

        builder.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("UQ_Guardians_UserId");

        builder.HasOne(e => e.User)
            .WithOne(u => u.Guardian!)
            .HasForeignKey<Guardian>(e => e.UserId)
            .HasConstraintName("FK_Guardians_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        builder.ToTable("StudentGuardians");

        builder.HasKey(e => new { e.StudentId, e.GuardianId }).HasName("PK_StudentGuardians");

        builder.Property(e => e.Relationship).HasMaxLength(30);
        builder.Property(e => e.ReceivesAlerts).HasDefaultValue(true);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.StudentGuardians)
            .HasForeignKey(e => e.StudentId)
            .HasConstraintName("FK_StudentGuardians_Students")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Guardian)
            .WithMany(g => g.StudentGuardians)
            .HasForeignKey(e => e.GuardianId)
            .HasConstraintName("FK_StudentGuardians_Guardians")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
