using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for AttendanceRecords: one row per student per session.</summary>
public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", t =>
        {
            t.HasCheckConstraint(
                "CK_AttendanceRecords_Status",
                "[Status] IN (N'Present', N'Late', N'Absent', N'Excused')");
            t.HasCheckConstraint(
                "CK_AttendanceRecords_Source",
                "[Source] IN (N'Scan', N'Auto', N'Manual')");
            t.HasCheckConstraint(
                "CK_AttendanceRecords_ScanTime",
                "[Source] <> N'Scan' OR [ScannedAt] IS NOT NULL");
        });

        builder.HasKey(a => a.AttendanceId);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Source).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(a => a.ScannedAt).HasColumnType("datetime2(0)");
        builder.Property(a => a.MarkedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => new { a.SessionId, a.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceRecords_Session_Student");

        builder.HasIndex(a => new { a.StudentId, a.Status })
            .HasDatabaseName("IX_AttendanceRecords_Student");

        builder.HasOne(a => a.Session)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.SessionId)
            .HasConstraintName("FK_AttendanceRecords_ClassSessions")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.StudentId)
            .HasConstraintName("FK_AttendanceRecords_Students")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.MarkedByUser)
            .WithMany()
            .HasForeignKey(a => a.MarkedBy)
            .HasConstraintName("FK_AttendanceRecords_MarkedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
