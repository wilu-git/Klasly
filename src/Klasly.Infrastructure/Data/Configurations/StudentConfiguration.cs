using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Students table (1-to-1 with Users).</summary>
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students", t => t.HasCheckConstraint(
            "CK_Students_YearLevel", "[YearLevel] BETWEEN 1 AND 6"));

        builder.HasKey(s => s.StudentId);

        builder.Property(s => s.StudentNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Program).HasMaxLength(50).IsRequired();

        builder.HasIndex(s => s.UserId).IsUnique().HasDatabaseName("UQ_Students_UserId");
        builder.HasIndex(s => s.StudentNumber).IsUnique().HasDatabaseName("UQ_Students_StudentNumber");

        builder.HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .HasConstraintName("FK_Students_Users")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
