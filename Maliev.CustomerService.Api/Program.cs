using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.CustomerService.Infrastructure.Persistence.Interceptors;
using Maliev.CustomerService.Infrastructure.Persistence.Repositories;
using Maliev.CustomerService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Maliev.CustomerService.Api.Program.Log.StartingHost(bootstrapLogger, "Customer Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("customers-meter"); // Register service meters for OpenTelemetry business metrics

    builder.AddStandardCache("customer:"); // Redis + in-memory fallback, memory-optimized
    builder.AddMassTransitWithRabbitMq(configure: x =>
    {
        x.AddConsumer<Maliev.CustomerService.Api.Consumers.GetCustomerDetailsConsumer>();
        x.AddConsumer<Maliev.CustomerService.Api.Consumers.FileDeletedEventConsumer>();
        x.AddConsumer<Maliev.CustomerService.Api.Consumers.OrderPaidEventConsumer>();
        x.AddConsumer<Maliev.CustomerService.Api.Consumers.SearchReindexRequestedConsumer>();
    }); // RabbitMQ message bus (non-blocking startup)
    builder.AddPostgresDbContext<CustomerDbContext>(connectionName: "CustomerDbContext"); // PostgreSQL with retry logic

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    builder.Services.AddMemoryCache(); // Required for CountryServiceClient caching

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    builder.AddStandardOpenApi(
        title: "MALIEV Customer Service API",
        description: "Comprehensive customer management service. Handles individual and corporate customer profiles, address books, NDA tracking, document management, and customer lifecycle status.");

    builder.Services.AddSingleton<Maliev.CustomerService.Api.Services.MetricsService>();

    // Security Services
    builder.Services.AddSingleton<Maliev.CustomerService.Application.Interfaces.IEncryptionService, EncryptionService>();
    builder.Services.AddSingleton<EncryptionInterceptor>();

    // Application Services
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICustomerService, Maliev.CustomerService.Api.Services.CustomerService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IAddressService, Maliev.CustomerService.Api.Services.AddressService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICompanyService, Maliev.CustomerService.Api.Services.CompanyService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.INDAService, Maliev.CustomerService.Api.Services.NDAService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IDocumentService, Maliev.CustomerService.Api.Services.DocumentService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IInternalNoteService, Maliev.CustomerService.Api.Services.InternalNoteService>();

    // Tier Calculation Services
    builder.Services.AddScoped<Maliev.CustomerService.Application.Interfaces.ICompanyRepository, CompanyRepository>();
    builder.Services.AddScoped<Maliev.CustomerService.Application.Interfaces.ICompanyTierSettingsRepository, CompanyTierSettingsRepository>();
    builder.Services.AddScoped<Maliev.CustomerService.Application.Interfaces.ICompanyDocumentRepository, CompanyDocumentRepository>();
    builder.Services.AddScoped<Maliev.CustomerService.Application.Interfaces.IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<Maliev.CustomerService.Application.Services.ITierCalculationService, Maliev.CustomerService.Application.Services.TierCalculationService>();
    builder.Services.AddScoped<Maliev.CustomerService.Application.Services.IYearEndTierProcessor, Maliev.CustomerService.Application.Services.YearEndTierProcessor>();

    // Scripts
    // builder.Services.AddScoped<Maliev.CustomerService.Api.Scripts.MigrateToPrincipalsScript>();

    // Background Services

    builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.NDAExpirationBackgroundService>();
    builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.DocumentDeletionRetryBackgroundService>();
    builder.Services.AddHostedService<Maliev.CustomerService.Application.BackgroundServices.YearEndTierJob>();

    // IAM Service Client (now with Service Account authentication)
    builder.AddIAMServiceClient("customer");
    builder.Services.AddIAMRegistration<CustomerIAMRegistrationService>("customer");
    builder.Services.AddHttpClient<Maliev.CustomerService.Api.Services.IIAMClient,
        Maliev.CustomerService.Api.Services.IAMClient>("CustomerService.IAM", (sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var explicitUrl = configuration["Services:IAMService:BaseUrl"];

            client.BaseAddress = new Uri(!string.IsNullOrEmpty(explicitUrl)
                ? explicitUrl
                : "https+http://IAMService");
            client.DefaultRequestHeaders.Add("X-Service-Name", "customer");
            client.Timeout = TimeSpan.FromSeconds(90);
        })
        .AddServiceDiscovery()
        .AddHttpMessageHandler<Maliev.Aspire.ServiceDefaults.IAM.ServiceAccountAuthenticationHandler>();

    // External Service Clients
    builder.AddAuthenticatedServiceClient<Maliev.CustomerService.Api.Services.External.ICountryServiceClient,
        Maliev.CustomerService.Api.Services.External.CountryServiceClient>("CountryService", sourceServiceName: "customer");

    builder.AddAuthenticatedServiceClient<Maliev.CustomerService.Api.Services.External.IUploadServiceClient,
        Maliev.CustomerService.Api.Services.External.UploadServiceClient>("UploadService", sourceServiceName: "customer");

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Rate Limiting (memory-optimized for low-spec nodes)
    builder.AddStandardRateLimiting();
    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Maliev.CustomerService.Api.Program>>();


    // Run database migrations on startup
    await app.MigrateDatabaseAsync<CustomerDbContext>();


    // Force instantiation of MetricsService to ensure OpenTelemetry meters are created
    var metricsService = app.Services.GetRequiredService<Maliev.CustomerService.Api.Services.MetricsService>();

    // Warm up database connection pool to avoid first-request timeout
    using var warmupScope = app.Services.CreateScope();
    var dbContext = warmupScope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
    logger.LogInformation("Database connection pool warmed up");

    // Middleware Pipeline
    app.UseStandardMiddleware();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseRouting();
    app.UseCors();
    app.UseRateLimiter();

    // Authentication and Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "customer");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "customer");

    Maliev.CustomerService.Api.Program.Log.ServiceStarted(logger, "Customer Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Maliev.CustomerService.Api.Program.Log.HostTerminated(bootstrapLogger, ex, "Customer Service");
    // Force flush to ensure Aspire captures the error before process exits
    Console.Out.Flush();
    Console.Error.Flush();
    throw;
}
finally
{
    loggerFactory.Dispose();
}

namespace Maliev.CustomerService.Api
{
    /// <summary>
    /// Represents the entry point and main application class for the program.
    /// </summary>
    public partial class Program
    {
        internal static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
            public static partial void StartingHost(ILogger logger, string serviceName);

            [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
            public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

            [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
            public static partial void ServiceStarted(ILogger logger, string serviceName);

            [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
            public static partial void MigrationFailed(ILogger logger, Exception exception);

            [LoggerMessage(Level = LogLevel.Information, Message = "OpenTelemetry metrics service initialized")]
            public static partial void MetricsInitialized(ILogger logger);
        }
    }
}
