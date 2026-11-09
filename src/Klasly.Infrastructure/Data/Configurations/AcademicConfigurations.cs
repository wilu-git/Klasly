using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

// Section 2 of Klasly_Schema.sql — academic structure.

public class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("AcademicTerms", t =>
            t.HasCheckConstraint("CK_AcademicTerms_Dates", "EndDate > StartDate"));

        builder.HasKey(e => e.TermId).HasName("PK_AcademicTerms");

        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StartDate).HasColumnType("date");
        builder.Property(e => e.EndDate).HasColumnType("date");
        builder.Property(e => e.IsActive).HasDefaultValue(false);

        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UQ_AcademicTerms_Name");

        // Only one term may be active at a time.
        builder.HasIndex(e => e.IsActive)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_AcademicTerms_OneActive");
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects", t =>
            t.HasCheckConstraint("CK_Subjects_Units", "Units BETWEEN 1 AND 10"));

        builder.HasKey(e => e.SubjectId).HasName("PK_Subjects");

        builder.Property(e => e.Code).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(150).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Subjects_Code");
    }
}

public class ClassOfferingConfiguration : IEntityTypeConfiguration<ClassOffering>
{
    public void Configure(EntityTypeBuilder<ClassOffering> builder)
    {
        builder.ToTable("Classes");

        builder.HasKey(e => e.ClassId).HasName("PK_Classes");

        builder.Property(e => e.Section).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Schedule).HasMaxLength(100);
        builder.Property(e => e.Room).HasMaxLength(30);

        builder.HasIndex(e => new { e.SubjectId, e.Section, e.TermId })
            .IsUnique()
            .HasDatabaseName("UQ_Classes_Subject_Section_Term");

        builder.HasOne(e => e.Subject)
            .WithMany(s => s.Classes)
            .HasForeignKey(e => e.SubjectId)
            .HasConstraintName("FK_Classes_Subjects")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Teacher)
            .WithMany(t => t.Classes)
            .HasForeignKey(e => e.TeacherId)
            .HasConstraintName("FK_Classes_Teachers")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Term)
            .WithMany(t => t.Classes)
            .HasForeignKey(e => e.TermId)
            .HasConstraintName("FK_Classes_AcademicTerms")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", t =>
            t.HasCheckConstraint("CK_Enrollments_Status", "Status IN (N'Active', N'Dropped')"));

        builder.HasKey(e => e.EnrollmentId).HasName("PK_Enrollments");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Core.Enums.EnrollmentStatus.Active)
            .IsRequired();

        builder.HasIndex(e => new { e.ClassId, e.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_Enrollments_Class_Student");

        builder.HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId)
            .HasConstraintName("FK_Enrollments_Classes")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .HasConstraintName("FK_Enrollments_Students")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
