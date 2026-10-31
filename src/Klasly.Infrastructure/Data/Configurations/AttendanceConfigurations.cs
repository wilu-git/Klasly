using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

// Section 3 of Klasly_Schema.sql — attendance.

public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("ClassSessions", t =>
        {
            t.HasCheckConstraint("CK_ClassSessions_Times", "EndTime > StartTime");
            t.HasCheckConstraint("CK_ClassSessions_Late", "LateAfterMinutes BETWEEN 0 AND 120");
            t.HasCheckConstraint("CK_ClassSessions_Status",
                "Status IN (N'Scheduled', N'Open', N'Closed', N'Finalized', N'Cancelled')");
        });

        builder.HasKey(e => e.SessionId).HasName("PK_ClassSessions");

        builder.Property(e => e.StartTime).HasColumnType("datetime2(0)");
        builder.Property(e => e.EndTime).HasColumnType("datetime2(0)");
        builder.Property(e => e.LateAfterMinutes).HasDefaultValue(15);
        builder.Property(e => e.QrNonce).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Core.Enums.SessionStatus.Scheduled)
            .IsRequired();
        builder.Property(e => e.ClosedAt).HasColumnType("datetime2(0)");

        // Worker query: find Open sessions whose EndTime has passed.
        builder.HasIndex(e => new { e.Status, e.EndTime })
            .HasDatabaseName("IX_ClassSessions_Status_EndTime");

        builder.HasOne(e => e.Class)
            .WithMany(c => c.Sessions)
            .HasForeignKey(e => e.ClassId)
            .HasConstraintName("FK_ClassSessions_Classes")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", t =>
        {
            t.HasCheckConstraint("CK_AttendanceRecords_Status",
                "Status IN (N'Present', N'Late', N'Absent', N'Excused')");
            t.HasCheckConstraint("CK_AttendanceRecords_Source",
                "Source IN (N'Scan', N'Auto', N'Manual')");
            t.HasCheckConstraint("CK_AttendanceRecords_ScanTime",
                "Source <> N'Scan' OR ScannedAt IS NOT NULL");
        });

        builder.HasKey(e => e.AttendanceId).HasName("PK_AttendanceRecords");

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ScannedAt).HasColumnType("datetime2(0)");
        builder.Property(e => e.MarkedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(10).IsRequired();

        // Blocks duplicate scans.
        builder.HasIndex(e => new { e.SessionId, e.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceRecords_Session_Student");

        // Student dashboard and risk job: attendance by student.
        builder.HasIndex(e => new { e.StudentId, e.Status })
            .HasDatabaseName("IX_AttendanceRecords_Student");

        builder.HasOne(e => e.Session)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(e => e.SessionId)
            .HasConstraintName("FK_AttendanceRecords_ClassSessions")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(e => e.StudentId)
            .HasConstraintName("FK_AttendanceRecords_Students")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.MarkedByUser)
            .WithMany(u => u.MarkedAttendanceRecords)
            .HasForeignKey(e => e.MarkedBy)
            .HasConstraintName("FK_AttendanceRecords_MarkedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class ExcuseRequestConfiguration : IEntityTypeConfiguration<ExcuseRequest>
{
    public void Configure(EntityTypeBuilder<ExcuseRequest> builder)
    {
        builder.ToTable("ExcuseRequests", t =>
            t.HasCheckConstraint("CK_ExcuseRequests_Status",
                "Status IN (N'Pending', N'Approved', N'Rejected')"));

        builder.HasKey(e => e.ExcuseId).HasName("PK_ExcuseRequests");

        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.AttachmentPath).HasMaxLength(300);
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Core.Enums.ExcuseStatus.Pending)
            .IsRequired();
        builder.Property(e => e.ReviewedAt).HasColumnType("datetime2(0)");

        // 1-to-0..1 with AttendanceRecords.
        builder.HasIndex(e => e.AttendanceId)
            .IsUnique()
            .HasDatabaseName("UQ_ExcuseRequests_Attendance");

        builder.HasOne(e => e.Attendance)
            .WithOne(a => a.ExcuseRequest!)
            .HasForeignKey<ExcuseRequest>(e => e.AttendanceId)
            .HasConstraintName("FK_ExcuseRequests_Attendance")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ReviewedByUser)
            .WithMany(u => u.ReviewedExcuseRequests)
            .HasForeignKey(e => e.ReviewedBy)
            .HasConstraintName("FK_ExcuseRequests_ReviewedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
