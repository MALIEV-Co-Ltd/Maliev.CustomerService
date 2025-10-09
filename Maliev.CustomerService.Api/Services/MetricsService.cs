using Prometheus;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service for collecting business metrics in Prometheus format
/// </summary>
public class MetricsService
{
    private readonly string _serviceName;
    private readonly string _environment;

    // Customer metrics
    private static readonly Gauge CustomerTotal = Metrics.CreateGauge(
        "customer_total",
        "Total customers by segment and tier",
        new GaugeConfiguration { LabelNames = new[] { "segment", "tier", "service_name", "environment" } });

    private static readonly Counter CustomerRegistrations = Metrics.CreateCounter(
        "customer_registrations_total",
        "Total customer registrations",
        new CounterConfiguration { LabelNames = new[] { "segment", "service_name", "environment" } });

    private static readonly Counter CustomerUpdates = Metrics.CreateCounter(
        "customer_updates_total",
        "Total customer updates",
        new CounterConfiguration { LabelNames = new[] { "actor_type", "service_name", "environment" } });

    // Auth metrics
    private static readonly Counter AuthValidation = Metrics.CreateCounter(
        "auth_validation_total",
        "Authentication validation attempts",
        new CounterConfiguration { LabelNames = new[] { "result", "service_name", "environment" } });

    private static readonly Histogram AuthValidationDuration = Metrics.CreateHistogram(
        "auth_validation_duration_seconds",
        "Authentication validation duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service_name", "environment" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });

    // NDA metrics
    private static readonly Gauge NdaTotal = Metrics.CreateGauge(
        "nda_total",
        "Total NDAs by status",
        new GaugeConfiguration { LabelNames = new[] { "status", "service_name", "environment" } });

    private static readonly Counter NdaTransitions = Metrics.CreateCounter(
        "nda_transitions_total",
        "NDA state transitions",
        new CounterConfiguration { LabelNames = new[] { "from_status", "to_status", "service_name", "environment" } });

    // Document metrics
    private static readonly Gauge DocumentTotal = Metrics.CreateGauge(
        "document_total",
        "Total documents by status",
        new GaugeConfiguration { LabelNames = new[] { "status", "service_name", "environment" } });

    private static readonly Counter DocumentOperations = Metrics.CreateCounter(
        "document_operations_total",
        "Document operations",
        new CounterConfiguration { LabelNames = new[] { "operation", "service_name", "environment" } });

    private static readonly Counter DocumentDeletionRetry = Metrics.CreateCounter(
        "document_deletion_retry_total",
        "Document deletion retry operations",
        new CounterConfiguration { LabelNames = new[] { "result", "service_name", "environment" } });

    // Company metrics
    private static readonly Gauge CompanyTotal = Metrics.CreateGauge(
        "company_total",
        "Total companies by tier",
        new GaugeConfiguration { LabelNames = new[] { "tier", "service_name", "environment" } });

    public MetricsService(IConfiguration configuration)
    {
        _serviceName = "customer-service";
        _environment = configuration["Environment"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "development";
    }

    // Customer metrics methods
    public void SetCustomerTotal(string segment, string tier, double value)
    {
        CustomerTotal.WithLabels(segment, tier, _serviceName, _environment).Set(value);
    }

    public void RecordCustomerRegistration(string segment)
    {
        CustomerRegistrations.WithLabels(segment, _serviceName, _environment).Inc();
    }

    public void RecordCustomerUpdate(string actorType)
    {
        CustomerUpdates.WithLabels(actorType, _serviceName, _environment).Inc();
    }

    // Auth metrics methods
    public void RecordAuthValidation(bool success)
    {
        AuthValidation.WithLabels(success ? "success" : "failure", _serviceName, _environment).Inc();
    }

    public IDisposable MeasureAuthValidationDuration()
    {
        return AuthValidationDuration.WithLabels(_serviceName, _environment).NewTimer();
    }

    // NDA metrics methods
    public void SetNdaTotal(string status, double value)
    {
        NdaTotal.WithLabels(status, _serviceName, _environment).Set(value);
    }

    public void RecordNdaTransition(string fromStatus, string toStatus)
    {
        NdaTransitions.WithLabels(fromStatus, toStatus, _serviceName, _environment).Inc();
    }

    // Document metrics methods
    public void SetDocumentTotal(string status, double value)
    {
        DocumentTotal.WithLabels(status, _serviceName, _environment).Set(value);
    }

    public void RecordDocumentOperation(string operation)
    {
        DocumentOperations.WithLabels(operation, _serviceName, _environment).Inc();
    }

    public void RecordDocumentDeletionRetry(bool success)
    {
        DocumentDeletionRetry.WithLabels(success ? "success" : "failure", _serviceName, _environment).Inc();
    }

    // Company metrics methods
    public void SetCompanyTotal(string tier, double value)
    {
        CompanyTotal.WithLabels(tier, _serviceName, _environment).Set(value);
    }
}
