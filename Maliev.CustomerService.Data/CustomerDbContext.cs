using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Maliev.CustomerService.Data.Models;
using Maliev.Aspire.ServiceDefaults.Database;

namespace Maliev.CustomerService.Data;

/// <summary>
/// Database context for Customer Service with ASP.NET Core Identity
/// </summary>
public class CustomerDbContext : IdentityDbContext<ApplicationUser>
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    // DbSets for application entities
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<NDARecord> NDARecords => Set<NDARecord>();
    public DbSet<DocumentReference> DocumentReferences => Set<DocumentReference>();
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer entity (T023)
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraints for segment and tier enums
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_customer_segment",
                    $"segment IN ('{CustomerSegment.Retail}', '{CustomerSegment.Wholesale}', '{CustomerSegment.Enterprise}', '{CustomerSegment.Government}')");

                t.HasCheckConstraint("ck_customer_tier",
                    $"tier IN ('{CustomerTier.Bronze}', '{CustomerTier.Silver}', '{CustomerTier.Gold}', '{CustomerTier.Platinum}', '{CustomerTier.VIP}')");
            });

            // Unique index on email WHERE is_deleted = false
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("is_deleted = false")
                .HasDatabaseName("ix_customer_email_unique_active");

            // Indexes for performance
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("ix_customer_company_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_customer_created_at");
            entity.HasIndex(e => e.Segment).HasDatabaseName("ix_customer_segment");
            entity.HasIndex(e => e.Tier).HasDatabaseName("ix_customer_tier");
            entity.HasIndex(e => e.PreferredLanguage).HasDatabaseName("ix_customer_preferred_language");

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure AuditLog entity (T024)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Composite index on entity_type and entity_id for quick lookups
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("ix_auditlog_entity_type_entity_id");

            // Index on actor_id for actor-based queries
            entity.HasIndex(e => e.ActorId).HasDatabaseName("ix_auditlog_actor_id");

            // Index on timestamp for time-based queries
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("ix_auditlog_timestamp");

            // Index on actor_type for filtering by actor type
            entity.HasIndex(e => e.ActorType).HasDatabaseName("ix_auditlog_actor_type");
        });

        // Configure Address entity (T037)
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraints for owner_type and type enums
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_address_owner_type",
                    $"owner_type IN ('{OwnerType.Customer}', '{OwnerType.Company}')");

                t.HasCheckConstraint("ck_address_type",
                    $"type IN ('{AddressType.Billing}', '{AddressType.Shipping}')");
            });

            // Composite index on (owner_type, owner_id) for efficient owner-based queries
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId })
                .HasDatabaseName("ix_address_owner_type_owner_id");

            // Index on country_id for country-based queries
            entity.HasIndex(e => e.CountryId).HasDatabaseName("ix_address_country_id");

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure ApplicationUser entity (T050)
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            // Index on last_login_at for inactive account detection queries
            entity.HasIndex(e => e.LastLoginAt).HasDatabaseName("ix_aspnetusers_last_login_at");

            // Index on linked_customer_id for customer-user linkage queries
            entity.HasIndex(e => e.LinkedCustomerId).HasDatabaseName("ix_aspnetusers_linked_customer_id");

            // Index on is_active for filtering active/inactive users
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_aspnetusers_is_active");

            // Index on created_at for time-based queries
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_aspnetusers_created_at");
        });

        // Configure Company entity (T066)
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraints for segment and tier enums
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_company_segment",
                    $"segment IN ('{CustomerSegment.Retail}', '{CustomerSegment.Wholesale}', '{CustomerSegment.Enterprise}', '{CustomerSegment.Government}')");

                t.HasCheckConstraint("ck_company_tier",
                    $"tier IN ('{CustomerTier.Bronze}', '{CustomerTier.Silver}', '{CustomerTier.Gold}', '{CustomerTier.Platinum}', '{CustomerTier.VIP}')");
            });

            // Index on vat_number for VAT-based queries
            entity.HasIndex(e => e.VatNumber).HasDatabaseName("ix_company_vat_number");

            // Index on name for name-based searches
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_company_name");

            // Index on segment for segment-based filtering
            entity.HasIndex(e => e.Segment).HasDatabaseName("ix_company_segment");

            // Index on tier for tier-based filtering
            entity.HasIndex(e => e.Tier).HasDatabaseName("ix_company_tier");

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure NDARecord entity (T078)
        modelBuilder.Entity<NDARecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraint for status enum
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_ndarecord_status",
                    $"status IN ('{NDAStatus.Draft}', '{NDAStatus.Signed}', '{NDAStatus.Expired}', '{NDAStatus.Revoked}')");
            });

            // Index on customer_id for customer-based queries
            entity.HasIndex(e => e.CustomerId).HasDatabaseName("ix_ndarecord_customer_id");

            // Index on status for status-based filtering
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_ndarecord_status");

            // Index on expires_at for expiration checking
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_ndarecord_expires_at");

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure DocumentReference entity (T091)
        modelBuilder.Entity<DocumentReference>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraints for owner_type and status enums
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_documentreference_owner_type",
                    $"owner_type IN ('{OwnerType.Customer}', '{OwnerType.Company}')");

                t.HasCheckConstraint("ck_documentreference_status",
                    $"status IN ('{DocumentStatus.Pending}', '{DocumentStatus.Complete}', '{DocumentStatus.PendingDeletion}', '{DocumentStatus.Orphaned}', '{DocumentStatus.MissingFile}')");
            });

            // Composite index on (owner_type, owner_id) for efficient owner-based queries
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId })
                .HasDatabaseName("ix_documentreference_owner_type_owner_id");

            // Index on document_type for document type filtering
            entity.HasIndex(e => e.DocumentType).HasDatabaseName("ix_documentreference_document_type");

            // Index on status for status-based filtering (e.g., PendingDeletion for background jobs)
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_documentreference_status");

            // Concurrency token
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure InternalNote entity (T107)
        modelBuilder.Entity<InternalNote>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure table with CHECK constraint for owner_type enum
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_internalnote_owner_type",
                    $"owner_type IN ('{OwnerType.Customer}', '{OwnerType.Company}')");
            });

            // Composite index on (owner_type, owner_id) for efficient owner-based queries
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId })
                .HasDatabaseName("ix_internalnote_owner_type_owner_id");

            // Index on created_at for time-based sorting
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_internalnote_created_at");

            // Index on created_by for filtering by author
            entity.HasIndex(e => e.CreatedBy).HasDatabaseName("ix_internalnote_created_by");

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Additional convention configurations can be added here
    }
}
