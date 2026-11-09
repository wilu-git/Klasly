using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Guardians table (1-to-1 with Users).</summary>
public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians");

        builder.HasKey(g => g.GuardianId);

        builder.Property(g => g.ContactEmail).HasMaxLength(256).IsRequired();
        builder.Property(g => g.ContactNumber).HasMaxLength(20);

        builder.HasIndex(g => g.UserId).IsUnique().HasDatabaseName("UQ_Guardians_UserId");

        builder.HasOne(g => g.User)
            .WithOne(u => u.Guardian)
            .HasForeignKey<Guardian>(g => g.UserId)
            .HasConstraintName("FK_Guardians_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
