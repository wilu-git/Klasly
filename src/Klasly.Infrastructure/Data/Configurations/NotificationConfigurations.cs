using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

// Section 5 of Klasly_Schema.sql — notifications (outbox) and settings.

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint("CK_Notifications_Channel", "Channel IN (N'Push', N'Email')");
            t.HasCheckConstraint("CK_Notifications_Status", "Status IN (N'Pending', N'Accepted', N'Failed')");
            t.HasCheckConstraint("CK_Notifications_Type", "Type IN (N'Absence', N'AtRisk', N'GradePosted')");
            t.HasCheckConstraint("CK_Notifications_Attempts", "Attempts >= 0");
        });

        builder.HasKey(e => e.NotificationId).HasName("PK_Notifications");

        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(200);
        builder.Property(e => e.Body).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Core.Enums.NotificationStatus.Pending)
            .IsRequired();
        builder.Property(e => e.Attempts).HasDefaultValue(0);
        builder.Property(e => e.NextAttemptAt).HasColumnType("datetime2(0)");
        builder.Property(e => e.LastError).HasMaxLength(500);
        builder.Property(e => e.DedupKey).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ExternalMessageId).HasMaxLength(200);
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        // Never send the same alert twice.
        builder.HasIndex(e => e.DedupKey).IsUnique().HasDatabaseName("UQ_Notifications_DedupKey");

        // Worker query: WHERE Status = 'Pending' AND NextAttemptAt <= SYSUTCDATETIME()
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt }).HasDatabaseName("IX_Notifications_Outbox");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("FK_Notifications_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(e => e.Key).HasName("PK_SystemSettings");

        builder.Property(e => e.Key).HasColumnName("Key").HasMaxLength(50).ValueGeneratedNever();
        builder.Property(e => e.Value).HasColumnName("Value").HasMaxLength(200).IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany(u => u.UpdatedSettings)
            .HasForeignKey(e => e.UpdatedBy)
            .HasConstraintName("FK_SystemSettings_UpdatedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
