using Klasly.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klasly.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for AcademicTerms, including the one-active-term filtered index.</summary>
public class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("AcademicTerms", t => t.HasCheckConstraint(
            "CK_AcademicTerms_Dates", "[EndDate] > [StartDate]"));

        builder.HasKey(t => t.TermId);

        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
        builder.Property(t => t.StartDate).HasColumnType("date");
        builder.Property(t => t.EndDate).HasColumnType("date");
        builder.Property(t => t.IsActive).HasDefaultValue(false);

        builder.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UQ_AcademicTerms_Name");

        // Only one term may be active at a time.
        builder.HasIndex(t => t.IsActive)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_AcademicTerms_OneActive");
    }
}
