using Maliev.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.CustomerService.Data.Configurations;

/// <summary>
/// EF Core configuration for Company entity
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Company> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.VatNumber)
            .HasMaxLength(50);

        entity.Property(e => e.RegistrationNumber)
            .HasMaxLength(50);

        entity.Property(e => e.ContactEmail)
            .HasMaxLength(255);

        entity.Property(e => e.ContactPhone)
            .HasMaxLength(20);

        entity.Property(e => e.Segment)
            .HasMaxLength(50);

        entity.Property(e => e.Tier)
            .HasMaxLength(50);

        entity.Property(e => e.CurrentYearPurchaseValue)
            .HasColumnType("decimal(18,2)");

        entity.HasIndex(e => e.VatNumber)
            .IsUnique()
            .HasFilter("\"VatNumber\" IS NOT NULL");

        entity.HasIndex(e => e.Tier);
    }
}
