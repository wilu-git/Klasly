using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Teachers table (1-to-1 with Users).</summary>
public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");

        builder.HasKey(t => t.TeacherId);

        builder.Property(t => t.EmployeeNo).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Department).HasMaxLength(100);

        builder.HasIndex(t => t.UserId).IsUnique().HasDatabaseName("UQ_Teachers_UserId");
        builder.HasIndex(t => t.EmployeeNo).IsUnique().HasDatabaseName("UQ_Teachers_EmployeeNo");

        builder.HasOne(t => t.User)
            .WithOne(u => u.Teacher)
            .HasForeignKey<Teacher>(t => t.UserId)
            .HasConstraintName("FK_Teachers_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
