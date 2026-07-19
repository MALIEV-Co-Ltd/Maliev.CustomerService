using Maliev.CustomerService.Api.Search;
using Maliev.CustomerService.Domain.Authorization;
using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Tests for mapping customer records into global search documents.
/// </summary>
public class CustomerSearchDocumentMapperTests
{
    /// <summary>
    /// Customer search upserts should expose customer names, company context, and the detail-page permission.
    /// </summary>
    [Fact]
    public void ToUpsertEvent_WithCustomerAndCompany_MapsSearchDocument()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            FirstName = "Kanya",
            LastName = "Larsson",
            Email = "kanya@example.com",
            Mobile = "+66812345678",
            Status = CustomerLifecycleStatus.Lead,
            Segment = "Enterprise",
            Tier = "VIP",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok",
            CompanyId = Guid.NewGuid()
        };
        var company = new Company
        {
            Id = customer.CompanyId.Value,
            Name = "Wanasriwilai Engineering",
            VatNumber = "TH-0123456789",
            ContactEmail = "hello@wanasriwilai.example"
        };
        var occurredAtUtc = DateTimeOffset.UtcNow;

        var message = CustomerSearchDocumentMapper.ToUpsertEvent(customer, company, occurredAtUtc);

        Assert.Equal("CustomerService", message.Payload.SourceService);
        Assert.Equal("customer", message.Payload.ResourceType);
        Assert.Equal(customer.Id.ToString(), message.Payload.ResourceId);
        Assert.Equal("Kanya Larsson", message.Payload.Title);
        Assert.Contains("kanya@example.com", message.Payload.Subtitle);
        Assert.Contains("Wanasriwilai Engineering", message.Payload.Subtitle);
        Assert.Contains("Kanya", message.Payload.Keywords);
        Assert.Contains("Wanasriwilai Engineering", message.Payload.Keywords);
        Assert.Equal("Lead", message.Payload.Status);
        Assert.Equal(CustomerPermissions.CustomersRead, message.Payload.RequiredPermission);
        Assert.Equal(occurredAtUtc, message.Payload.OccurredAtUtc);
    }

    /// <summary>
    /// Customer delete events should tombstone the same indexed document key.
    /// </summary>
    [Fact]
    public void ToDeletedEvent_WithCustomerId_MapsSearchDocumentKey()
    {
        var customerId = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.UtcNow;

        var message = CustomerSearchDocumentMapper.ToDeletedEvent(customerId, occurredAtUtc);

        Assert.Equal("CustomerService", message.Payload.SourceService);
        Assert.Equal("customer", message.Payload.ResourceType);
        Assert.Equal(customerId.ToString(), message.Payload.ResourceId);
        Assert.Equal(occurredAtUtc, message.Payload.OccurredAtUtc);
    }
}
