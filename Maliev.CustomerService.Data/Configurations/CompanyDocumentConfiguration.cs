using Maliev.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.CustomerService.Data.Configurations;

/// <summary>
/// EF Core configuration for CompanyDocument entity
/// </summary>
public class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CompanyDocument> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.FileUrl)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(e => e.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        entity.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.CompanyId);
    }
}
