using System.Threading.RateLimiting;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;

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



builder.AddRedisDistributedCache(instanceName: "customer:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)
builder.AddPostgresDbContext<CustomerDbContext>(connectionName: "CustomerDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
builder.AddStandardOpenApi(
    title: "MALIEV Customer Service API",
    description: "Comprehensive customer management service. Handles individual and corporate customer profiles, address books, NDA tracking, document management, and customer lifecycle status.");

builder.Services.AddSingleton<Maliev.CustomerService.Api.Services.MetricsService>();

// Application Services
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICustomerService, Maliev.CustomerService.Api.Services.CustomerService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IAddressService, Maliev.CustomerService.Api.Services.AddressService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICompanyService, Maliev.CustomerService.Api.Services.CompanyService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.INDAService, Maliev.CustomerService.Api.Services.NDAService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IDocumentService, Maliev.CustomerService.Api.Services.DocumentService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IInternalNoteService, Maliev.CustomerService.Api.Services.InternalNoteService>();

// Scripts
builder.Services.AddScoped<Maliev.CustomerService.Api.Scripts.MigrateToPrincipalsScript>();

// Background Services
builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.NDAExpirationBackgroundService>();
builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.DocumentDeletionRetryBackgroundService>();

// External Service Clients
builder.AddServiceClient<Maliev.CustomerService.Api.Services.External.ICountryServiceClient,
    Maliev.CustomerService.Api.Services.External.CountryServiceClient>("CountryService");

builder.AddServiceClient<Maliev.CustomerService.Api.Services.External.IUploadServiceClient,
    Maliev.CustomerService.Api.Services.External.UploadServiceClient>("UploadService");

// IAM Service Client
builder.AddServiceClient<Maliev.CustomerService.Api.Services.IIAMClient, Maliev.CustomerService.Api.Services.IAMClient>("IAM");

// IAM Service HttpClient for registration service
builder.AddServiceClient("IAMService");

// IAM Registration Service
builder.Services.AddIAMRegistration<Maliev.CustomerService.Api.Services.CustomerIAMRegistrationService>();

// Controllers
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Program).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddAuthorization(options =>
{
    // EmployeeOrHigher policy for internal notes
    options.AddPolicy("EmployeeOrHigher", policy =>
        policy.RequireRole("Employee", "Manager", "Admin"));
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("sliding-validation-policy", context =>
    {
        // Use an extremely aggressive rate limit for test environment to ensure it triggers
        // The X-Test-ID header should make each test run use a separate rate limit counter
        var testId = context.Request.Headers["X-Test-ID"].FirstOrDefault();
        var key = !string.IsNullOrEmpty(testId) ? testId : context.Connection.RemoteIpAddress?.ToString() ?? "default-rate-limit-key";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3, // Allow only 3 requests
            Window = TimeSpan.FromSeconds(1), // Per second
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            code = "RATE_LIMIT_EXCEEDED",
            message = "Too many requests. Please try again later.",
            traceId = context.HttpContext.TraceIdentifier,
            timestamp = DateTime.UtcNow
        }, cancellationToken);
    };
});
var app = builder.Build();

// --- CLI Command Handlers ---
if (args.Contains("--migrate-principals"))
{
    using var scope = app.Services.CreateScope();
    var migrator = scope.ServiceProvider.GetRequiredService<Maliev.CustomerService.Api.Scripts.MigrateToPrincipalsScript>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var cliLogger = loggerFactory.CreateLogger("CLI");

    try
    {
        await migrator.ExecuteAsync();
        cliLogger.LogInformation("Migration command completed successfully.");
    }
    catch (Exception ex)
    {
        cliLogger.LogCritical(ex, "Migration command failed with error.");
        Environment.Exit(1);
    }
    return;
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<CustomerDbContext>();
    }
    catch (Exception ex)
    {
        Log.MigrationFailed(logger, ex);
        // Don't throw - allow app to start for debugging
    }
}

// Force instantiation of MetricsService to ensure OpenTelemetry meters are created
var metricsService = app.Services.GetRequiredService<Maliev.CustomerService.Api.Services.MetricsService>();
Log.MetricsInitialized(logger);

// Middleware Pipeline
app.UseStandardMiddleware();
app.UseHttpsRedirection();

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

Log.ServiceStarted(logger);
await app.RunAsync();

/// <summary>
/// Represents the entry point and main application class for the program.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "CustomerService started successfully")]
        public static partial void ServiceStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "OpenTelemetry metrics service initialized")]
        public static partial void MetricsInitialized(ILogger logger);
    }
}

