using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Api.Middleware;
using FluentValidation;
using Polly;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (Console only)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting Maliev Customer Service API");

try
{
    // Secrets from Google Secret Manager
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
    }

    // Database Context (skipped in Testing - configured by TestWebApplicationFactory)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        var connectionString = builder.Configuration.GetConnectionString("CustomerDbContext")
            ?? builder.Configuration["CustomerDbContext"]  // Alternative for env var format
            ?? throw new InvalidOperationException("Database connection string 'CustomerDbContext' not found.");

        builder.Services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
    }

    // ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy (T063 - will be configured in Phase 5)
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

    // Memory Cache (simple configuration per CLAUDE.md)
    builder.Services.AddMemoryCache();

    // FluentValidation (T029-T030)
    builder.Services.AddValidatorsFromAssemblyContaining<Maliev.CustomerService.Api.Validators.CreateCustomerRequestValidator>();

    // Prometheus metrics (Constitution Principle X)
    builder.Services.AddSingleton<Maliev.CustomerService.Api.Services.MetricsService>();

    // Application Services (T031-T032, T046-T047, T058-T059, T072-T073, T084-T085, T100-T101, T114-T115)
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICustomerService, Maliev.CustomerService.Api.Services.CustomerService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IAddressService, Maliev.CustomerService.Api.Services.AddressService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IUserService, Maliev.CustomerService.Api.Services.UserService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.ICompanyService, Maliev.CustomerService.Api.Services.CompanyService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.INDAService, Maliev.CustomerService.Api.Services.NDAService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IDocumentService, Maliev.CustomerService.Api.Services.DocumentService>();
    builder.Services.AddScoped<Maliev.CustomerService.Api.Services.IInternalNoteService, Maliev.CustomerService.Api.Services.InternalNoteService>();

    // Background Services (T087-T088, T103-T104)
    builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.NDAExpirationBackgroundService>();
    builder.Services.AddHostedService<Maliev.CustomerService.Api.BackgroundServices.DocumentDeletionRetryBackgroundService>();

    // External Service Clients (T044-T045, T097-T099)
    builder.Services.Configure<Maliev.CustomerService.Api.Configuration.CountryServiceOptions>(
        builder.Configuration.GetSection(Maliev.CustomerService.Api.Configuration.CountryServiceOptions.SectionName));

    builder.Services.AddHttpClient<Maliev.CustomerService.Api.Services.External.ICountryServiceClient,
        Maliev.CustomerService.Api.Services.External.CountryServiceClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Maliev.CustomerService.Api.Configuration.CountryServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });

    builder.Services.Configure<Maliev.CustomerService.Api.Configuration.UploadServiceOptions>(
        builder.Configuration.GetSection("ExternalServices:UploadService"));

    builder.Services.AddHttpClient<Maliev.CustomerService.Api.Services.External.IUploadServiceClient,
        Maliev.CustomerService.Api.Services.External.UploadServiceClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Maliev.CustomerService.Api.Configuration.UploadServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

    // Controllers - explicitly add this assembly as ApplicationPart
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(Program).Assembly)
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Health Checks (T017)
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CustomerDbContext>(tags: new[] { "readiness" });

    // JWT Bearer Authentication (T013) - RSA Public Key Validation
    // Note: In Testing environment, TestWebApplicationFactory replaces this with FakeAuthenticationHandler
    var publicKeyBase64 = builder.Configuration["Jwt:PublicKey"] ?? "test-key";
    var issuer = builder.Configuration["Jwt:Issuer"] ?? "test-issuer";
    var audience = builder.Configuration["Jwt:Audience"] ?? "test-audience";

    if (publicKeyBase64 != "test-key")
    {
        // Production: Use RSA public key validation
        // The public key from Google Secret Manager is double base64-encoded PEM format
        // First decode to get the PEM string
        var pemString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64));

        // Extract base64 content from PEM format (remove headers)
        var base64Key = pemString
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        // Decode the base64 content to get raw DER bytes
        var keyBytes = Convert.FromBase64String(base64Key);

        // Import the DER-formatted public key
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        var securityKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = securityKey,
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.MapInboundClaims = false; // Keep original claim names
            });
    }
    else
    {
        // Testing: Use default authentication (will be replaced by FakeAuthenticationHandler)
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
    }

    builder.Services.AddAuthorization(options =>
    {
        // EmployeeOrHigher policy for internal notes (T113)
        options.AddPolicy("EmployeeOrHigher", policy =>
            policy.RequireRole("Employee", "Manager", "Admin"));
    });

    // Rate Limiting (T014)
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

    // Swagger/OpenAPI (T018)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Maliev Customer Service API",
            Version = "v1",
            Description = "Customer management microservice with address, user account, company, NDA, document, and notes management"
        });

        // JWT Bearer configuration for Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Configure base path for all routes
    app.UsePathBase("/customers");

    // Middleware Pipeline (EXACT ORDER per CLAUDE.md)
    app.UseExceptionHandling(); // T016

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./v1/swagger.json", "Maliev Customer Service API v1");
        c.RoutePrefix = "swagger";
    });

    // Skip HTTPS redirection in test environment (causes "Failed to determine the https port" warning)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    app.UseRouting(); // Enable endpoint routing

    // Authentication and Authorization middleware (always enabled, uses FakeAuthenticationHandler in Testing)
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    // HTTP metrics middleware (Constitution Principle X)
    app.UseHttpMetrics();

    // Health check endpoints (T017) - UsePathBase adds /customers prefix
    app.MapGet("/liveness", () => "Healthy")
        .AllowAnonymous()
        .WithName("Liveness");

    app.MapHealthChecks("/readiness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Metrics endpoint (Constitution Principle X)
    app.MapMetrics("/metrics").AllowAnonymous();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to integration tests
public partial class Program { }
