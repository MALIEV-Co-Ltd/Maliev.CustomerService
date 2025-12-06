using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service for collecting business metrics using OpenTelemetry
/// </summary>
public class MetricsService : IDisposable
{
    private readonly Meter _meter;
    private readonly string _serviceName;
    private readonly string _environment;

    // Counters (monotonic - only increase)
    private readonly Counter<long> _customerRegistrations;
    private readonly Counter<long> _customerUpdates;
    private readonly Counter<long> _authValidation;
    private readonly Counter<long> _ndaTransitions;
    private readonly Counter<long> _documentOperations;
    private readonly Counter<long> _documentDeletionRetry;

    // Histograms (for distributions)
    private readonly Histogram<double> _authValidationDuration;

    // Gauges (using ObservableGauge with state tracking)
    private long _customerTotalValue;
    private long _ndaTotalValue;
    private long _documentTotalValue;
    private long _companyTotalValue;

    /// <summary>
    /// Initializes a new instance of the MetricsService class
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    public MetricsService(IConfiguration configuration)
    {
        _serviceName = "customer-service";
        _environment = configuration["Environment"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "development";

        // Create a meter for this service
        _meter = new Meter(_serviceName, "1.0.0");

        // Initialize counters
        _customerRegistrations = _meter.CreateCounter<long>(
            "customer.registrations",
            description: "Total customer registrations");

        _customerUpdates = _meter.CreateCounter<long>(
            "customer.updates",
            description: "Total customer updates");

        _authValidation = _meter.CreateCounter<long>(
            "auth.validation",
            description: "Authentication validation attempts");

        _ndaTransitions = _meter.CreateCounter<long>(
            "nda.transitions",
            description: "NDA state transitions");

        _documentOperations = _meter.CreateCounter<long>(
            "document.operations",
            description: "Document operations");

        _documentDeletionRetry = _meter.CreateCounter<long>(
            "document.deletion.retry",
            description: "Document deletion retry operations");

        // Initialize histograms
        _authValidationDuration = _meter.CreateHistogram<double>(
            "auth.validation.duration",
            unit: "s",
            description: "Authentication validation duration in seconds");

        // Initialize observable gauges
        _meter.CreateObservableGauge(
            "customer.total",
            () => new Measurement<long>(_customerTotalValue, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Total customers");

        _meter.CreateObservableGauge(
            "nda.total",
            () => new Measurement<long>(_ndaTotalValue, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Total NDAs");

        _meter.CreateObservableGauge(
            "document.total",
            () => new Measurement<long>(_documentTotalValue, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Total documents");

        _meter.CreateObservableGauge(
            "company.total",
            () => new Measurement<long>(_companyTotalValue, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Total companies");

        // Initialize all counters with zero to ensure they appear in metrics output
        InitializeMetrics();
    }

    /// <summary>
    /// Initializes all metrics with zero values to ensure they appear in Prometheus output
    /// </summary>
    private void InitializeMetrics()
    {
        // Initialize counters with zero (required for OpenTelemetry to export them)
        _customerRegistrations.Add(0, new KeyValuePair<string, object?>("segment", "unknown"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _customerUpdates.Add(0, new KeyValuePair<string, object?>("actor_type", "unknown"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _authValidation.Add(0, new KeyValuePair<string, object?>("result", "success"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _authValidation.Add(0, new KeyValuePair<string, object?>("result", "failure"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _ndaTransitions.Add(0, new KeyValuePair<string, object?>("from_status", "unknown"), new KeyValuePair<string, object?>("to_status", "unknown"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _documentOperations.Add(0, new KeyValuePair<string, object?>("operation", "unknown"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _documentDeletionRetry.Add(0, new KeyValuePair<string, object?>("result", "success"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
        _documentDeletionRetry.Add(0, new KeyValuePair<string, object?>("result", "failure"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));

        // Initialize histogram with a zero-duration measurement
        _authValidationDuration.Record(0, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));

        // Gauges are initialized via their observable callbacks (already set to 0 via Interlocked fields)
    }

    // Customer metrics methods
    /// <summary>
    /// Sets the total customer count gauge metric
    /// </summary>
    /// <param name="segment">Customer segment</param>
    /// <param name="tier">Customer tier</param>
    /// <param name="value">Total count value</param>
    public void SetCustomerTotal(string segment, string tier, long value)
    {
        Interlocked.Exchange(ref _customerTotalValue, value);
    }

    /// <summary>
    /// Records a customer registration event
    /// </summary>
    /// <param name="segment">Customer segment</param>
    public void RecordCustomerRegistration(string segment)
    {
        _customerRegistrations.Add(1, new KeyValuePair<string, object?>("segment", segment), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records a customer update event
    /// </summary>
    /// <param name="actorType">Type of actor performing the update</param>
    public void RecordCustomerUpdate(string actorType)
    {
        _customerUpdates.Add(1, new KeyValuePair<string, object?>("actor_type", actorType), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    // Auth metrics methods
    /// <summary>
    /// Records an authentication validation attempt
    /// </summary>
    /// <param name="success">True if validation succeeded, false otherwise</param>
    public void RecordAuthValidation(bool success)
    {
        _authValidation.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failure"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records authentication validation duration
    /// </summary>
    /// <param name="durationSeconds">Duration in seconds</param>
    public void RecordAuthValidationDuration(double durationSeconds)
    {
        _authValidationDuration.Record(durationSeconds, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Creates a stopwatch to measure authentication validation duration
    /// </summary>
    /// <returns>Stopwatch for measuring duration</returns>
    public Stopwatch MeasureAuthValidationDuration()
    {
        var stopwatch = Stopwatch.StartNew();
        return stopwatch;
    }

    /// <summary>
    /// Completes authentication validation measurement
    /// </summary>
    /// <param name="stopwatch">The stopwatch that was measuring</param>
    public void CompleteAuthValidationMeasurement(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        RecordAuthValidationDuration(stopwatch.Elapsed.TotalSeconds);
    }

    // NDA metrics methods
    /// <summary>
    /// Sets the total NDA count gauge metric
    /// </summary>
    /// <param name="status">NDA status</param>
    /// <param name="value">Total count value</param>
    public void SetNdaTotal(string status, long value)
    {
        Interlocked.Exchange(ref _ndaTotalValue, value);
    }

    /// <summary>
    /// Records an NDA state transition event
    /// </summary>
    /// <param name="fromStatus">Status transitioning from</param>
    /// <param name="toStatus">Status transitioning to</param>
    public void RecordNdaTransition(string fromStatus, string toStatus)
    {
        _ndaTransitions.Add(1, new KeyValuePair<string, object?>("from_status", fromStatus), new KeyValuePair<string, object?>("to_status", toStatus), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    // Document metrics methods
    /// <summary>
    /// Sets the total document count gauge metric
    /// </summary>
    /// <param name="status">Document status</param>
    /// <param name="value">Total count value</param>
    public void SetDocumentTotal(string status, long value)
    {
        Interlocked.Exchange(ref _documentTotalValue, value);
    }

    /// <summary>
    /// Records a document operation event
    /// </summary>
    /// <param name="operation">Operation type (create, update, delete, complete)</param>
    public void RecordDocumentOperation(string operation)
    {
        _documentOperations.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records a document deletion retry event
    /// </summary>
    /// <param name="success">True if retry succeeded, false otherwise</param>
    public void RecordDocumentDeletionRetry(bool success)
    {
        _documentDeletionRetry.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failure"), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    // Company metrics methods
    /// <summary>
    /// Sets the total company count gauge metric
    /// </summary>
    /// <param name="tier">Company tier</param>
    /// <param name="value">Total count value</param>
    public void SetCompanyTotal(string tier, long value)
    {
        Interlocked.Exchange(ref _companyTotalValue, value);
    }

    /// <summary>
    /// Disposes the meter
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
