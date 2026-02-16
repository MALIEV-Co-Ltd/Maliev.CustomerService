using System.Reflection;
using Maliev.CustomerService.Data.Attributes;
using Maliev.CustomerService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Maliev.CustomerService.Data.Interceptors;

/// <summary>
/// EF Core interceptor that automatically encrypts/decrypts properties marked with [Encrypted] attribute
/// Runs on SaveChanges (encrypt) and query materialization (decrypt)
/// </summary>
public class EncryptionInterceptor : SaveChangesInterceptor, IMaterializationInterceptor
{
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the EncryptionInterceptor class
    /// </summary>
    /// <param name="encryptionService">Encryption service for encrypting/decrypting data</param>
    public EncryptionInterceptor(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EncryptSensitiveData(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EncryptSensitiveData(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance)
    {
        // Simply decrypt the entity properties
        // Don't touch EF Core's change tracking at this stage
        DecryptSensitiveData(instance);
        return instance;
    }

    private void EncryptSensitiveData(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var encryptedProperties = GetEncryptedProperties(entityType);

            foreach (var property in encryptedProperties)
            {
                var efProperty = entry.Property(property.Name);
                var currentValue = property.GetValue(entry.Entity) as string;

                // Encrypt current value if it's plain text
                if (!string.IsNullOrEmpty(currentValue) && !_encryptionService.IsEncrypted(currentValue))
                {
                    var encryptedValue = _encryptionService.Encrypt(currentValue);

                    // Set CurrentValue to encrypted for database save
                    efProperty.CurrentValue = encryptedValue;

                    // CRITICAL: For Added entities, also set OriginalValue to avoid re-encryption
                    // For Modified entities, keep OriginalValue as-is (from database)
                    if (entry.State == EntityState.Added)
                    {
                        efProperty.OriginalValue = encryptedValue;
                    }

                    // Keep entity property as plaintext for in-memory operations
                    // This allows the same context to be reused without reloading entities
                }
            }
        }
    }

    private void DecryptSensitiveData(object entity)
    {
        if (entity == null) return;

        var entityType = entity.GetType();
        var encryptedProperties = GetEncryptedProperties(entityType);

        foreach (var property in encryptedProperties)
        {
            var currentValue = property.GetValue(entity) as string;
            if (!string.IsNullOrEmpty(currentValue) && _encryptionService.IsEncrypted(currentValue))
            {
                var decryptedValue = _encryptionService.Decrypt(currentValue);
                property.SetValue(entity, decryptedValue);
            }
        }
    }

    /// <summary>
    /// Gets all properties marked with [Encrypted] attribute from an entity type
    /// </summary>
    private static IEnumerable<PropertyInfo> GetEncryptedProperties(Type entityType)
    {
        return entityType.GetProperties()
            .Where(p => p.PropertyType == typeof(string) &&
                        p.GetCustomAttribute<EncryptedAttribute>() != null);
    }
}
