using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using Maliev.CustomerService.Api.Middleware; // Keep if CorrelationIdMiddleware is kept
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path =>
        path.StartsWith("/health") || path.StartsWith("/metrics")))
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/customer-service-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Maliev Customer Service");

    // Load secrets.yaml (keep if secrets are used for anything, e.g., connection strings for health checks)
    builder.Configuration.AddYamlFile("secrets.yaml", optional: true, reloadOnChange: true);

    // Load secrets from mounted volume in GKE (keep if deployed in GKE)
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
    }

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, Maliev.CustomerService.Api.Configurations.ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen();

    // Keep basic health check
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            c.SwaggerEndpoint($"./{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        c.RoutePrefix = "customers/swagger";
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>(); // Keep if desired for error handling
    app.UseHttpsRedirection();

    app.MapHealthChecks("/customers/liveness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("liveness")
    });

    app.MapHealthChecks("/customers/readiness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

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

// Make Program class accessible for integration tests
public partial class Program
{ }