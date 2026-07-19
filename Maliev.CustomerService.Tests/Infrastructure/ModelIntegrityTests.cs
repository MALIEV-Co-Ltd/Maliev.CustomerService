using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.CustomerService.Infrastructure.Persistence.Interceptors;
using Maliev.CustomerService.Infrastructure.Security;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Verifies that the EF Core model matches the current migrations.
/// This prevents "Pending model changes" exceptions at runtime.
/// </summary>
public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        // Use a dummy connection string just to build the model for comparison
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        // Create encryption service for testing
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            })
            .Build();
        IEncryptionService encryptionService = new EncryptionService(configuration);
        var encryptionInterceptor = new EncryptionInterceptor(encryptionService);

        using var context = new CustomerDbContext(options, encryptionService, encryptionInterceptor);

        // This helper (available in EF Core 9.0+) checks if the current code
        // matches the last snapshot in the Migrations folder.
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges,
            "The EF Core model for 'CustomerDbContext' has changed but no migration has been added. " +
            "Run 'dotnet ef migrations add <Name> --project Maliev.CustomerService.Infrastructure --startup-project Maliev.CustomerService.Api' to fix this.");
    }

    [Fact]
    public void Model_ShouldIncludeMassTransitOutboxEntities()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseNpgsql("Host=localhost;Database=customer_model_test;Username=postgres;Password=postgres")
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            })
            .Build();
        IEncryptionService encryptionService = new EncryptionService(configuration);
        var encryptionInterceptor = new EncryptionInterceptor(encryptionService);

        using var context = new CustomerDbContext(options, encryptionService, encryptionInterceptor);

        Assert.NotNull(context.Model.FindEntityType(typeof(InboxState)));
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxMessage)));
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxState)));
    }
}
