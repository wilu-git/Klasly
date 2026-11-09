using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for ClassSessions, including the index the Worker uses to find ended sessions.</summary>
public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("ClassSessions", t =>
        {
            t.HasCheckConstraint("CK_ClassSessions_Times", "[EndTime] > [StartTime]");
            t.HasCheckConstraint("CK_ClassSessions_Late", "[LateAfterMinutes] BETWEEN 0 AND 120");
            t.HasCheckConstraint(
                "CK_ClassSessions_Status",
                "[Status] IN (N'Scheduled', N'Open', N'Closed', N'Finalized', N'Cancelled')");
        });

        builder.HasKey(s => s.SessionId);

        builder.Property(s => s.StartTime).HasColumnType("datetime2(0)");
        builder.Property(s => s.EndTime).HasColumnType("datetime2(0)");
        builder.Property(s => s.LateAfterMinutes).HasDefaultValue(15);
        builder.Property(s => s.QrNonce).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SessionStatus.Scheduled)
            .IsRequired();
        builder.Property(s => s.ClosedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(s => new { s.Status, s.EndTime })
            .HasDatabaseName("IX_ClassSessions_Status_EndTime");

        builder.HasOne(s => s.Class)
            .WithMany(c => c.ClassSessions)
            .HasForeignKey(s => s.ClassId)
            .HasConstraintName("FK_ClassSessions_Classes")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
