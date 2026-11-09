using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Assessments table.</summary>
public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments", t =>
        {
            t.HasCheckConstraint("CK_Assessments_MaxScore", "[MaxScore] > 0");
            t.HasCheckConstraint("CK_Assessments_Weight", "[WeightPercent] > 0 AND [WeightPercent] <= 100");
        });

        builder.HasKey(a => a.AssessmentId);

        builder.Property(a => a.Title).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Type).HasMaxLength(30).IsRequired();
        builder.Property(a => a.MaxScore).HasPrecision(6, 2);
        builder.Property(a => a.WeightPercent).HasPrecision(5, 2);
        builder.Property(a => a.DatePosted).HasColumnType("datetime2(0)");

        builder.HasIndex(a => new { a.ClassId, a.Title })
            .IsUnique()
            .HasDatabaseName("UQ_Assessments_Class_Title");

        builder.HasOne(a => a.Class)
            .WithMany(c => c.Assessments)
            .HasForeignKey(a => a.ClassId)
            .HasConstraintName("FK_Assessments_Classes")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
