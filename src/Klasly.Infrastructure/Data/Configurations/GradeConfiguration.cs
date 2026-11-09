using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Grades table. Score &lt;= MaxScore is checked in the Web API.</summary>
public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades", t => t.HasCheckConstraint("CK_Grades_Score", "[Score] >= 0"));

        builder.HasKey(g => g.GradeId);

        builder.Property(g => g.Score).HasPrecision(6, 2);
        builder.Property(g => g.PostedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(g => g.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(g => new { g.AssessmentId, g.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_Grades_Assessment_Student");

        builder.HasIndex(g => g.StudentId).HasDatabaseName("IX_Grades_Student");

        builder.HasOne(g => g.Assessment)
            .WithMany(a => a.Grades)
            .HasForeignKey(g => g.AssessmentId)
            .HasConstraintName("FK_Grades_Assessments")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.StudentId)
            .HasConstraintName("FK_Grades_Students")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
