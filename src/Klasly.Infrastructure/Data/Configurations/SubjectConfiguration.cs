using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Subjects table.</summary>
public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects", t => t.HasCheckConstraint(
            "CK_Subjects_Units", "[Units] BETWEEN 1 AND 10"));

        builder.HasKey(s => s.SubjectId);

        builder.Property(s => s.Code).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Title).HasMaxLength(150).IsRequired();

        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("UQ_Subjects_Code");
    }
}
