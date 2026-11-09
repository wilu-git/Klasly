using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for StudentGuardians, the M:N join table with a composite key.</summary>
public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        builder.ToTable("StudentGuardians");

        builder.HasKey(sg => new { sg.StudentId, sg.GuardianId });

        builder.Property(sg => sg.Relationship).HasMaxLength(30);
        builder.Property(sg => sg.ReceivesAlerts).HasDefaultValue(true);

        builder.HasOne(sg => sg.Student)
            .WithMany(s => s.StudentGuardians)
            .HasForeignKey(sg => sg.StudentId)
            .HasConstraintName("FK_StudentGuardians_Students")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(sg => sg.Guardian)
            .WithMany(g => g.StudentGuardians)
            .HasForeignKey(sg => sg.GuardianId)
            .HasConstraintName("FK_StudentGuardians_Guardians")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
