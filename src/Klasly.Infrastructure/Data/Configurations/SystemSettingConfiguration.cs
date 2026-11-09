using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for SystemSettings; Key is a reserved word, so the column stays bracketed in SQL.</summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(50);
        builder.Property(s => s.Value).HasMaxLength(200).IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasOne(s => s.UpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.UpdatedBy)
            .HasConstraintName("FK_SystemSettings_UpdatedBy")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
