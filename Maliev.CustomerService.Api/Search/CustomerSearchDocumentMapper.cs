using Maliev.CustomerService.Domain.Authorization;
using Maliev.CustomerService.Domain.Entities;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;

namespace Maliev.CustomerService.Api.Search;

/// <summary>
/// Maps customer records to centralized global search documents.
/// </summary>
public static class CustomerSearchDocumentMapper
{
    private const string SourceService = "CustomerService";
    private const string ResourceType = "customer";

    /// <summary>
    /// Creates a search upsert event for a customer.
    /// </summary>
    /// <param name="customer">Customer to index.</param>
    /// <param name="company">Optional company linked to the customer.</param>
    /// <param name="occurredAtUtc">Timestamp for the source change.</param>
    /// <returns>A centralized search upsert event.</returns>
    public static SearchDocumentUpsertedEvent ToUpsertEvent(Customer customer, Company? company, DateTimeOffset occurredAtUtc)
    {
        var title = $"{customer.FirstName} {customer.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = customer.Email;
        }

        var subtitle = CompactKeywords(customer.Email, company?.Name).ToArray();
        var summary = string.Join(" ",
            customer.Status,
            customer.Segment,
            customer.Tier,
            customer.Mobile,
            customer.Landline,
            customer.PreferredLanguage,
            customer.Timezone,
            company?.Name,
            company?.VatNumber)
            .Trim();

        var keywords = CompactKeywords(
            customer.Id.ToString(),
            customer.PrincipalId.ToString(),
            customer.FirstName,
            customer.LastName,
            title,
            customer.Email,
            customer.Mobile,
            customer.Landline,
            customer.Extension,
            customer.Segment,
            customer.Tier,
            customer.Status,
            customer.PreferredLanguage,
            customer.Timezone,
            customer.CompanyId?.ToString(),
            company?.Name,
            company?.VatNumber,
            company?.RegistrationNumber,
            company?.ContactEmail,
            company?.ContactPhone)
            .ToArray();

        return new SearchDocumentUpsertedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchDocumentUpsertedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: SourceService,
            ConsumedBy: ["SearchService"],
            CorrelationId: customer.Id,
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchDocumentUpsertedEventPayload(
                SourceService: SourceService,
                ResourceType: ResourceType,
                ResourceId: customer.Id.ToString(),
                Title: title,
                Subtitle: subtitle.Length == 0 ? null : string.Join(" - ", subtitle),
                Summary: string.IsNullOrWhiteSpace(summary) ? null : summary,
                Keywords: keywords,
                Status: customer.IsDeleted ? CustomerLifecycleStatus.Inactive : customer.Status,
                RequiredPermission: CustomerPermissions.CustomersRead,
                OccurredAtUtc: occurredAtUtc));
    }

    /// <summary>
    /// Creates a search delete event for a customer.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="occurredAtUtc">Timestamp for the source change.</param>
    /// <returns>A centralized search delete event.</returns>
    public static SearchDocumentDeletedEvent ToDeletedEvent(Guid customerId, DateTimeOffset occurredAtUtc)
    {
        return new SearchDocumentDeletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchDocumentDeletedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: SourceService,
            ConsumedBy: ["SearchService"],
            CorrelationId: customerId,
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchDocumentDeletedEventPayload(
                SourceService: SourceService,
                ResourceType: ResourceType,
                ResourceId: customerId.ToString(),
                OccurredAtUtc: occurredAtUtc));
    }

    private static IEnumerable<string> CompactKeywords(params string?[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
