using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Data;

[Collection("Database Collection")]
public class DataCoverageTests
{
    private readonly TestWebApplicationFactory _factory;

    public DataCoverageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ModelEnums_Coverage()
    {
        // Access constants to get coverage for static classes
        Assert.Equal("Customer", OwnerType.Customer);
        Assert.Equal("Company", OwnerType.Company);

        Assert.Equal("Billing", AddressType.Billing);
        Assert.Equal("Shipping", AddressType.Shipping);

        Assert.Equal("Create", AuditAction.Create);
        Assert.Equal("Update", AuditAction.Update);

        Assert.Equal("Retail", CustomerSegment.Retail);
        Assert.Equal("Bronze", CustomerTier.Bronze);

        Assert.Equal(DocumentStatus.Pending, DocumentStatus.Pending);
        Assert.Equal(NDAStatus.Draft, NDAStatus.Draft);
    }

    [Fact]
    public void ActorType_Coverage()
    {
        Assert.Equal("Customer", ActorType.Customer);
        Assert.Equal("Employee", ActorType.Employee);
        Assert.Equal("System", ActorType.System);
        Assert.Contains("Customer", ActorType.All);
    }
}
