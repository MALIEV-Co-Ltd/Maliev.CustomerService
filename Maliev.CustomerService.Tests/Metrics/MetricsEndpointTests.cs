using System.Net;
using FluentAssertions;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Metrics;

/// <summary>
/// Tests for Prometheus metrics endpoint
/// Validates that metrics endpoint is available, returns proper format, and doesn't expose PII
/// </summary>
[Collection("Database Collection")]
public class MetricsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MetricsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsPrometheusFormat()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Contain("text/plain");

        // Should contain standard HTTP metrics from prometheus-net.AspNetCore
        content.Should().Contain("http_requests_received_total");
        content.Should().Contain("http_request_duration_seconds");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsCustomerMetrics()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Customer metrics
        content.Should().Contain("customer_total");
        content.Should().Contain("customer_registrations_total");
        content.Should().Contain("customer_updates_total");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsAuthMetrics()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Auth metrics
        content.Should().Contain("auth_validation_total");
        content.Should().Contain("auth_validation_duration_seconds");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsNdaMetrics()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // NDA metrics
        content.Should().Contain("nda_total");
        content.Should().Contain("nda_transitions_total");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsDocumentMetrics()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Document metrics
        content.Should().Contain("document_total");
        content.Should().Contain("document_operations_total");
        content.Should().Contain("document_deletion_retry_total");
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsRequiredLabels()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Required labels should appear in metrics
        content.Should().Contain("service_name=\"customer-service\"");
        content.Should().Contain("environment=");
    }

    [Fact]
    public async Task MetricsEndpoint_DoesNotExposePII()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should NOT contain email patterns
        content.Should().NotContain("@");

        // Should NOT contain common PII patterns
        content.Should().NotMatchRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"); // email
        content.Should().NotMatchRegex(@"\+\d{10,15}"); // phone number in E.164 format
    }

    [Fact]
    public async Task MetricsEndpoint_IsAccessibleWithoutAuthentication()
    {
        // Arrange - Create client WITHOUT authentication headers
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Metrics endpoint should be anonymous (AllowAnonymous)
    }
}
