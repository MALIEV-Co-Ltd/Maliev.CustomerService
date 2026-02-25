using Maliev.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.CustomerService.Data.Configurations;

/// <summary>
/// EF Core configuration for CompanyTierSettings entity
/// </summary>
public class CompanyTierSettingsConfiguration : IEntityTypeConfiguration<CompanyTierSettings>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CompanyTierSettings> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.TierName)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.MinPurchaseValue)
            .HasColumnType("decimal(18,2)");

        entity.Property(e => e.DiscountPercentage)
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.FreeShippingMinOrder)
            .HasColumnType("decimal(18,2)");

        entity.Property(e => e.CoinRewardPercentage)
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        entity.HasIndex(e => e.TierName);
        entity.HasIndex(e => new { e.TierName, e.ValidFrom });
    }
}
