using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Scripts;

/// <summary>
/// One-time migration script to create principals for existing customers in IAM
/// </summary>
public class MigrateToPrincipalsScript
{
    private readonly CustomerDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly ILogger<MigrateToPrincipalsScript> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateToPrincipalsScript"/> class
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="iamClient">IAM service client</param>
    /// <param name="logger">Logger instance</param>
    public MigrateToPrincipalsScript(
        CustomerDbContext context,
        IIAMClient iamClient,
        ILogger<MigrateToPrincipalsScript> logger)
    {
        _context = context;
        _iamClient = iamClient;
        _logger = logger;
    }

    /// <summary>
    /// Executes the migration backfill process
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting customer to IAM principal migration backfill...");

        var customersToMigrate = await _context.Customers
            .Where(c => c.PrincipalId == Guid.Empty && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} customers needing migration", customersToMigrate.Count);

        int successCount = 0;
        int failureCount = 0;
        int batchSize = 100;

        for (int i = 0; i < customersToMigrate.Count; i += batchSize)
        {
            var batch = customersToMigrate.Skip(i).Take(batchSize).ToList();
            _logger.LogInformation("Processing batch {BatchIndex} ({BatchSize} customers)...", (i / batchSize) + 1, batch.Count);

            foreach (var customer in batch)
            {
                try
                {
                    var response = await _iamClient.CreatePrincipalAsync(new CreatePrincipalRequest
                    {
                        Email = customer.Email,
                        DisplayName = $"{customer.FirstName} {customer.LastName}",
                        PrincipalType = "user",
                        LinkedService = "CustomerService"
                    }, cancellationToken);

                    customer.PrincipalId = response.PrincipalId;
                    customer.UpdatedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate customer {CustomerId} (Email: {Email})", customer.Id, customer.Email);
                    failureCount++;
                }
            }

            // Save batch changes
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully saved batch. Progress: {Success}/{Total}", successCount, customersToMigrate.Count);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to save changes for batch starting at index {Index}", i);
                throw;
            }
        }

        _logger.LogInformation("Migration backfill complete. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount, failureCount);
    }
}
