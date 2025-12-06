using System.Threading.RateLimiting;
using Maliev.CustomerService.Api.Middleware;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("customer-service"); // Register service meters for OpenTelemetry business metrics

builder.AddRedisDistributedCache(instanceName: "Customer:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)
builder.AddPostgresDbContext<CustomerDbContext>(connectionStringName: "CustomerDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Customer Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Customer relationship management service. Manages customer profiles with addresses and contact information, company entities, user accounts linked to customers, NDA document tracking, internal notes, and document attachments with validation endpoints for service integration.";
            return Task.CompletedTask;
        });
    });
}

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout policy
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<CustomerDbContext>()
.AddDefaultTokenProviders();

// Ensure JWT is the default scheme (Identity overrides it to Cookies by default)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
});

// OpenTelemetry business metrics
builder.Services.AddSingleton<Maliev.CustomerService.Api.Services.MetricsService>();

// Application Services
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICustomerService, Maliev.CustomerService.Api.Services.CustomerService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IAddressService, Maliev.CustomerService.Api.Services.AddressService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IUserService, Maliev.CustomerService.Api.Services.UserService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICompanyService, Maliev.CustomerService.Api.Services.CompanyService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.INDAService, Maliev.CustomerService.Api.Services.NDAService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IDocumentService, Maliev.CustomerService.Api.Services.DocumentService>();
builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IInternalNoteService, Maliev.CustomerService.Api.Services.InternalNoteService>();

// Background Services
builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.NDAExpirationBackgroundService>();
builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.DocumentDeletionRetryBackgroundService>();

// External Service Clients
builder.Services.Configure<Maliev.CustomerService.Api.Configuration.CountryServiceOptions>(
    builder.Configuration.GetSection(Maliev.CustomerService.Api.Configuration.CountryServiceOptions.SectionName));
builder.Services.AddHttpClient<Maliev.CustomerService.Api.Services.External.ICountryServiceClient,
    Maliev.CustomerService.Api.Services.External.CountryServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.CustomerService.Api.Configuration.UploadServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:UploadService"));
builder.Services.AddHttpClient<Maliev.CustomerService.Api.Services.External.IUploadServiceClient,
    Maliev.CustomerService.Api.Services.External.UploadServiceClient>()
    .AddStandardResilienceHandler();

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
    // General rate limit: 100 requests per minute
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Special rate limit for /validate endpoint: 10 requests per minute
        if (context.Request.Path.StartsWithSegments("/customers/v1/validate"))
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                context.Request.Headers.Host.ToString(),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
        }

        // Default rate limit for all other endpoints
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Request.Headers.Host.ToString(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
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
app.UseExceptionHandling();

// Skip HTTPS redirection in test environment
if (!app.Environment.IsEnvironment("Testing"))
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
app.MapDefaultEndpoints(servicePrefix: "customers");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "customers");

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

