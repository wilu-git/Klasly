using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

// Section 4 of Klasly_Schema.sql — grades and risk.

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments", t =>
        {
            t.HasCheckConstraint("CK_Assessments_MaxScore", "MaxScore > 0");
            t.HasCheckConstraint("CK_Assessments_Weight", "WeightPercent > 0 AND WeightPercent <= 100");
        });

        builder.HasKey(e => e.AssessmentId).HasName("PK_Assessments");

        builder.Property(e => e.Title).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(30).IsRequired();
        builder.Property(e => e.MaxScore).HasPrecision(6, 2);
        builder.Property(e => e.WeightPercent).HasPrecision(5, 2);
        builder.Property(e => e.DatePosted).HasColumnType("datetime2(0)");

        builder.HasIndex(e => new { e.ClassId, e.Title })
            .IsUnique()
            .HasDatabaseName("UQ_Assessments_Class_Title");

        builder.HasOne(e => e.Class)
            .WithMany(c => c.Assessments)
            .HasForeignKey(e => e.ClassId)
            .HasConstraintName("FK_Assessments_Classes")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades", t =>
            t.HasCheckConstraint("CK_Grades_Score", "Score >= 0"));

        builder.HasKey(e => e.GradeId).HasName("PK_Grades");

        builder.Property(e => e.Score).HasPrecision(6, 2);
        builder.Property(e => e.PostedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(e => new { e.AssessmentId, e.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_Grades_Assessment_Student");

        builder.HasIndex(e => e.StudentId).HasDatabaseName("IX_Grades_Student");

        builder.HasOne(e => e.Assessment)
            .WithMany(a => a.Grades)
            .HasForeignKey(e => e.AssessmentId)
            .HasConstraintName("FK_Grades_Assessments")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(e => e.StudentId)
            .HasConstraintName("FK_Grades_Students")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class RiskFlagConfiguration : IEntityTypeConfiguration<RiskFlag>
{
    public void Configure(EntityTypeBuilder<RiskFlag> builder)
    {
        builder.ToTable("RiskFlags", t =>
        {
            t.HasCheckConstraint("CK_RiskFlags_AbsenceRate", "AbsenceRate BETWEEN 0 AND 1");
            t.HasCheckConstraint("CK_RiskFlags_Average",
                "WeightedAverage IS NULL OR WeightedAverage BETWEEN 0 AND 100");
        });

        builder.HasKey(e => e.RiskFlagId).HasName("PK_RiskFlags");

        builder.Property(e => e.AbsenceRate).HasPrecision(5, 4);
        builder.Property(e => e.WeightedAverage).HasPrecision(5, 2);
        builder.Property(e => e.ComputedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => new { e.EnrollmentId, e.ComputedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_RiskFlags_Enrollment_Latest");

        builder.HasOne(e => e.Enrollment)
            .WithMany(en => en.RiskFlags)
            .HasForeignKey(e => e.EnrollmentId)
            .HasConstraintName("FK_RiskFlags_Enrollments")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
