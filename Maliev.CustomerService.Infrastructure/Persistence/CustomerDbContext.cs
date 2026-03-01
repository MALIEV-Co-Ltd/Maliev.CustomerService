using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence.Configurations;
using Maliev.CustomerService.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Infrastructure.Persistence;

/// <summary>
/// Database context for the Customer microservice.
/// Handles Customers, Companies, Addresses, NDAs, and Documents.
/// </summary>
public class CustomerDbContext : DbContext
{
    /// <summary>System principal ID</summary>
    public const string SystemPrincipalId = "00000000-0000-0000-0000-000000000001";

    private readonly IEncryptionService _encryptionService;
    private readonly EncryptionInterceptor _encryptionInterceptor;

    /// <summary>Initializes a new instance of the CustomerDbContext class</summary>
    public CustomerDbContext(
        DbContextOptions<CustomerDbContext> options,
        IEncryptionService encryptionService,
        EncryptionInterceptor encryptionInterceptor) : base(options)
    {
        _encryptionService = encryptionService;
        _encryptionInterceptor = encryptionInterceptor;
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.AddInterceptors(_encryptionInterceptor);
    }

    /// <summary>Customers set</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Companies set</summary>
    public DbSet<Company> Companies => Set<Company>();
    /// <summary>Addresses set</summary>
    public DbSet<Address> Addresses => Set<Address>();
    /// <summary>NDAs set</summary>
    public DbSet<NDARecord> NDARecords => Set<NDARecord>();
    /// <summary>Document references set</summary>
    public DbSet<DocumentReference> DocumentReferences => Set<DocumentReference>();
    /// <summary>Audit logs set</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    /// <summary>Internal notes set</summary>
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();
    /// <summary>Internal note comments set</summary>
    public DbSet<InternalNoteComment> InternalNoteComments => Set<InternalNoteComment>();
    /// <summary>Company tier settings set</summary>
    public DbSet<CompanyTierSettings> CompanyTierSettings => Set<CompanyTierSettings>();
    /// <summary>Company documents set</summary>
    public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyTierSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyDocumentConfiguration());

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PrincipalId).IsUnique();

            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure Address entity
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Column Ordering
            entity.Property(e => e.Id).HasColumnOrder(0);
            entity.Property(e => e.OwnerType).HasColumnOrder(1);
            entity.Property(e => e.OwnerId).HasColumnOrder(2);
            entity.Property(e => e.Type).HasColumnOrder(3);
            entity.Property(e => e.IsDefault).HasColumnOrder(4);
            entity.Property(e => e.AddressLine1).HasColumnOrder(5);
            entity.Property(e => e.AddressLine2).HasColumnOrder(6);
            entity.Property(e => e.AddressLine3).HasColumnOrder(7);
            entity.Property(e => e.District).HasColumnOrder(8);
            entity.Property(e => e.City).HasColumnOrder(9);
            entity.Property(e => e.StateProvince).HasColumnOrder(10);
            entity.Property(e => e.PostalCode).HasColumnOrder(11);
            entity.Property(e => e.CountryId).HasColumnOrder(12);
            entity.Property(e => e.CreatedAt).HasColumnOrder(13);
            entity.Property(e => e.UpdatedAt).HasColumnOrder(14);
            entity.Property(e => e.Version).HasColumnOrder(15);

            // Configure table with CHECK constraints for owner_type and type enums
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("ck_address_owner_type",
                    "\"OwnerType\" IN ('Customer', 'Company')");

                t.HasCheckConstraint("ck_address_type",
                    "\"Type\" IN ('Billing', 'Shipping')");
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

        // Configure NDARecord
        modelBuilder.Entity<NDARecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure DocumentReference
        modelBuilder.Entity<DocumentReference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ActorId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActorType).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure InternalNote
        modelBuilder.Entity<InternalNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });

            // Concurrency token (PostgreSQL bytea with default value)
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });

        // Configure InternalNoteComment
        modelBuilder.Entity<InternalNoteComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InternalNoteId);

            // Concurrency token
            entity.Property(e => e.Version)
                .IsRowVersion()
                .HasDefaultValueSql("'\\x0000000000000001'::bytea")
                .ValueGeneratedOnAddOrUpdate();
        });
    }
}
