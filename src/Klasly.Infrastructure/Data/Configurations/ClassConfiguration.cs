using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for Classes: one subject offering per section per term.</summary>
public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("Classes");

        builder.HasKey(c => c.ClassId);

        builder.Property(c => c.Section).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Schedule).HasMaxLength(100);
        builder.Property(c => c.Room).HasMaxLength(30);

        builder.HasIndex(c => new { c.SubjectId, c.Section, c.TermId })
            .IsUnique()
            .HasDatabaseName("UQ_Classes_Subject_Section_Term");

        builder.HasOne(c => c.Subject)
            .WithMany(s => s.Classes)
            .HasForeignKey(c => c.SubjectId)
            .HasConstraintName("FK_Classes_Subjects")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Teacher)
            .WithMany(t => t.Classes)
            .HasForeignKey(c => c.TeacherId)
            .HasConstraintName("FK_Classes_Teachers")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Term)
            .WithMany(t => t.Classes)
            .HasForeignKey(c => c.TermId)
            .HasConstraintName("FK_Classes_AcademicTerms")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
