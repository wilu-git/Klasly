using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for ExcuseRequests (1-to-0..1 with AttendanceRecords).</summary>
public class ExcuseRequestConfiguration : IEntityTypeConfiguration<ExcuseRequest>
{
    public void Configure(EntityTypeBuilder<ExcuseRequest> builder)
    {
        builder.ToTable("ExcuseRequests", t => t.HasCheckConstraint(
            "CK_ExcuseRequests_Status", "[Status] IN (N'Pending', N'Approved', N'Rejected')"));

        builder.HasKey(e => e.ExcuseId);

        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.AttachmentPath).HasMaxLength(300);
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExcuseStatus.Pending)
            .IsRequired();
        builder.Property(e => e.ReviewedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(e => e.AttendanceId).IsUnique().HasDatabaseName("UQ_ExcuseRequests_Attendance");

        builder.HasOne(e => e.Attendance)
            .WithOne(a => a.ExcuseRequest)
            .HasForeignKey<ExcuseRequest>(e => e.AttendanceId)
            .HasConstraintName("FK_ExcuseRequests_Attendance")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ReviewedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReviewedBy)
            .HasConstraintName("FK_ExcuseRequests_ReviewedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
