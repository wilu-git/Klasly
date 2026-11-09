using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for RiskFlags, the Worker snapshot table.</summary>
public class RiskFlagConfiguration : IEntityTypeConfiguration<RiskFlag>
{
    public void Configure(EntityTypeBuilder<RiskFlag> builder)
    {
        builder.ToTable("RiskFlags", t =>
        {
            t.HasCheckConstraint("CK_RiskFlags_AbsenceRate", "[AbsenceRate] BETWEEN 0 AND 1");
            t.HasCheckConstraint(
                "CK_RiskFlags_Average",
                "[WeightedAverage] IS NULL OR [WeightedAverage] BETWEEN 0 AND 100");
        });

        builder.HasKey(r => r.RiskFlagId);

        builder.Property(r => r.AbsenceRate).HasPrecision(5, 4);
        builder.Property(r => r.WeightedAverage).HasPrecision(5, 2);
        builder.Property(r => r.ComputedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        // Dashboards read the newest snapshot per enrollment.
        builder.HasIndex(r => new { r.EnrollmentId, r.ComputedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_RiskFlags_Enrollment_Latest");

        builder.HasOne(r => r.Enrollment)
            .WithMany(e => e.RiskFlags)
            .HasForeignKey(r => r.EnrollmentId)
            .HasConstraintName("FK_RiskFlags_Enrollments")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
