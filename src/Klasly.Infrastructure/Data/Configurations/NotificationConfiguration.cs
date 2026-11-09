using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for Notifications, the push/email outbox.</summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint("CK_Notifications_Channel", "[Channel] IN (N'Push', N'Email')");
            t.HasCheckConstraint("CK_Notifications_Status", "[Status] IN (N'Pending', N'Accepted', N'Failed')");
            t.HasCheckConstraint("CK_Notifications_Type", "[Type] IN (N'Absence', N'AtRisk', N'GradePosted')");
            t.HasCheckConstraint("CK_Notifications_Attempts", "[Attempts] >= 0");
        });

        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(200);
        builder.Property(n => n.Body).HasColumnType("nvarchar(max)");
        builder.Property(n => n.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(NotificationStatus.Pending)
            .IsRequired();
        builder.Property(n => n.Attempts).HasDefaultValue(0);
        builder.Property(n => n.NextAttemptAt).HasColumnType("datetime2(0)");
        builder.Property(n => n.LastError).HasMaxLength(500);
        builder.Property(n => n.DedupKey).HasMaxLength(150).IsRequired();
        builder.Property(n => n.ExternalMessageId).HasMaxLength(200);
        builder.Property(n => n.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(n => n.DedupKey).IsUnique().HasDatabaseName("UQ_Notifications_DedupKey");

        // Worker query: Status = 'Pending' AND NextAttemptAt <= SYSUTCDATETIME()
        builder.HasIndex(n => new { n.Status, n.NextAttemptAt }).HasDatabaseName("IX_Notifications_Outbox");

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .HasConstraintName("FK_Notifications_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
