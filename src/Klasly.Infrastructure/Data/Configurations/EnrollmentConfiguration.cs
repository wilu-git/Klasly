using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for Enrollments, the M:N link between students and classes.</summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", t => t.HasCheckConstraint(
            "CK_Enrollments_Status", "[Status] IN (N'Active', N'Dropped')"));

        builder.HasKey(e => e.EnrollmentId);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EnrollmentStatus.Active)
            .IsRequired();

        builder.HasIndex(e => new { e.ClassId, e.StudentId })
            .IsUnique()
            .HasDatabaseName("UQ_Enrollments_Class_Student");

        builder.HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId)
            .HasConstraintName("FK_Enrollments_Classes")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .HasConstraintName("FK_Enrollments_Students")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
