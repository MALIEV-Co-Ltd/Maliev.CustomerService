using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Reference data describing payment terms available for customer profiles.
/// </summary>
public class PaymentTerm
{
    /// <summary>
    /// Stable payment term code.
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable payment term label.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Payment term category used for grouping and filtering.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of how the payment term calculates the due date.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Guidance describing when this term is typically used.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TypicalUse { get; set; } = string.Empty;

    /// <summary>
    /// Number of calendar days until payment is due when the term is day-based.
    /// </summary>
    public int? DueDays { get; set; }

    /// <summary>
    /// Early payment discount percentage, when the term offers one.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Number of days the early payment discount is available.
    /// </summary>
    public int? DiscountDays { get; set; }

    /// <summary>
    /// Whether this payment term is the default for new customers.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Sort order for presenting payment terms.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Built-in payment term values.
/// </summary>
public static class PaymentTerms
{
    /// <summary>Due immediately on receipt.</summary>
    public const string DueOnReceipt = "Due on receipt";

    /// <summary>Payment due before work begins or goods are released.</summary>
    public const string Prepaid = "Prepaid";

    /// <summary>Payment due when the order is placed.</summary>
    public const string CashWithOrder = "Cash with order";

    /// <summary>Payment collected when goods are delivered.</summary>
    public const string CashOnDelivery = "Cash on delivery";

    /// <summary>Half paid before work starts and the balance before shipment.</summary>
    public const string FiftyDepositBalanceBeforeShip = "50% deposit, balance before shipment";

    /// <summary>Payment due in 7 days.</summary>
    public const string Net7 = "Net 7";

    /// <summary>Payment due in 10 days.</summary>
    public const string Net10 = "Net 10";

    /// <summary>Payment due in 15 days.</summary>
    public const string Net15 = "Net 15";

    /// <summary>Payment due in 30 days.</summary>
    public const string Net30 = "Net 30";

    /// <summary>Payment due in 45 days.</summary>
    public const string Net45 = "Net 45";

    /// <summary>Payment due in 60 days.</summary>
    public const string Net60 = "Net 60";

    /// <summary>Payment due in 90 days.</summary>
    public const string Net90 = "Net 90";

    /// <summary>Payment due at the end of the invoice month.</summary>
    public const string EndOfMonth = "End of month";

    /// <summary>Payment due 30 days after the end of the invoice month.</summary>
    public const string Net30Eom = "Net 30 EOM";

    /// <summary>Payment due 60 days after the end of the invoice month.</summary>
    public const string Net60Eom = "Net 60 EOM";

    /// <summary>Payment due on the 15th day of the following month.</summary>
    public const string FifteenthProximo = "15th of following month";

    /// <summary>One percent discount if paid in 10 days; otherwise due in 30 days.</summary>
    public const string OneTenNet30 = "1/10 Net 30";

    /// <summary>Two percent discount if paid in 10 days; otherwise due in 30 days.</summary>
    public const string TwoTenNet30 = "2/10 Net 30";

    /// <summary>Two percent discount if paid in 10 days; otherwise due in 60 days.</summary>
    public const string TwoTenNet60 = "2/10 Net 60";

    /// <summary>Invoices are issued and paid against agreed project milestones.</summary>
    public const string MilestoneProgress = "Milestone / progress payments";

    /// <summary>Payment split into scheduled installments.</summary>
    public const string Installments = "Installments";

    /// <summary>All built-in payment term names.</summary>
    public static string[] All => SeedData.Select(term => term.Name).ToArray();

    /// <summary>Seed data for built-in payment terms.</summary>
    public static readonly PaymentTerm[] SeedData =
    [
        Term("DUE_ON_RECEIPT", DueOnReceipt, "Immediate", "Payment is due as soon as the invoice is received.", "Use for one-off jobs, quick-turnaround work, or customers without approved credit.", 0, true, 0),
        Term("PREPAID", Prepaid, "Advance", "Full payment is required before work starts or goods are released.", "Use for new customers, high-risk accounts, custom materials, or orders that require committed cash before production.", 0, false, 5),
        Term("CASH_WITH_ORDER", CashWithOrder, "Advance", "Payment is collected when the order is placed.", "Use for ecommerce-like orders, small standard items, or orders that should not enter production without payment.", 0, false, 10),
        Term("CASH_ON_DELIVERY", CashOnDelivery, "Delivery", "Payment is collected when goods are delivered.", "Use for local delivery or pickup workflows where collection happens at handoff instead of at invoice issue.", 0, false, 15),
        Term("FIFTY_DEPOSIT_BALANCE_BEFORE_SHIP", FiftyDepositBalanceBeforeShip, "Deposit", "The customer pays 50% before production and the remaining 50% before shipment.", "Use for custom manufacturing, special-order materials, tooling, and larger jobs where MALIEV should not carry the full working-capital risk.", null, false, 20),
        Term("NET_7", Net7, "Net", "Full invoice amount is due 7 calendar days after the invoice date.", "Use for newer accounts or small businesses where cash collection should stay tight but short trade credit is acceptable.", 7, false, 25),
        Term("NET_10", Net10, "Net", "Full invoice amount is due 10 calendar days after the invoice date.", "Use for short trade-credit arrangements where the customer needs processing time but not a full month.", 10, false, 30),
        Term("NET_15", Net15, "Net", "Full invoice amount is due 15 calendar days after the invoice date.", "Use for repeat customers with modest credit exposure or jobs where two-week payment is commercially acceptable.", 15, false, 35),
        Term("NET_30", Net30, "Net", "Full invoice amount is due 30 calendar days after the invoice date.", "Use as the standard B2B trade-credit term for approved customers with normal purchasing cycles.", 30, false, 40),
        Term("NET_45", Net45, "Net", "Full invoice amount is due 45 calendar days after the invoice date.", "Use for larger commercial customers that require longer internal approval cycles.", 45, false, 45),
        Term("NET_60", Net60, "Net", "Full invoice amount is due 60 calendar days after the invoice date.", "Use only for established enterprise or government accounts where longer credit has been approved.", 60, false, 50),
        Term("NET_90", Net90, "Net", "Full invoice amount is due 90 calendar days after the invoice date.", "Use sparingly for strategic enterprise agreements because it materially increases working-capital exposure.", 90, false, 55),
        Term("END_OF_MONTH", EndOfMonth, "Calendar", "Payment is due by the last day of the month in which the invoice is issued.", "Use when the customer pays all invoices in a monthly closing cycle.", null, false, 60),
        Term("NET_30_EOM", Net30Eom, "Calendar", "Payment is due 30 days after the end of the invoice month.", "Use for customers whose accounts payable process calculates due dates from month-end rather than invoice date.", null, false, 65),
        Term("NET_60_EOM", Net60Eom, "Calendar", "Payment is due 60 days after the end of the invoice month.", "Use for approved enterprise accounts with month-end plus extended credit cycles.", null, false, 70),
        Term("FIFTEENTH_PROXIMO", FifteenthProximo, "Calendar", "Payment is due on the 15th day of the month after the invoice month.", "Use for customers that run a fixed mid-month payment cycle.", null, false, 75),
        Term("ONE_TEN_NET_30", OneTenNet30, "Discount", "Customer may deduct 1% if payment is received within 10 days; otherwise the full amount is due in 30 days.", "Use when a small early-payment incentive helps improve cash flow without heavily discounting margin.", 30, false, 80, 1m, 10),
        Term("TWO_TEN_NET_30", TwoTenNet30, "Discount", "Customer may deduct 2% if payment is received within 10 days; otherwise the full amount is due in 30 days.", "Use for approved accounts where faster cash collection is worth the discount.", 30, false, 85, 2m, 10),
        Term("TWO_TEN_NET_60", TwoTenNet60, "Discount", "Customer may deduct 2% if payment is received within 10 days; otherwise the full amount is due in 60 days.", "Use for large accounts that require longer net terms but can be encouraged to pay early.", 60, false, 90, 2m, 10),
        Term("MILESTONE_PROGRESS", MilestoneProgress, "Milestone", "Invoices are issued and paid as agreed project milestones are completed.", "Use for long-running engineering, tooling, fabrication, or project work where billing should follow delivery phases.", null, false, 95),
        Term("INSTALLMENTS", Installments, "Installment", "Payment is split into scheduled installments instead of one invoice due date.", "Use for high-value projects with a negotiated payment schedule that should be documented on the quote or contract.", null, false, 100)
    ];

    private static PaymentTerm Term(
        string code,
        string name,
        string category,
        string description,
        string typicalUse,
        int? dueDays,
        bool isDefault,
        int sortOrder,
        decimal? discountPercent = null,
        int? discountDays = null) => new()
        {
            Code = code,
            Name = name,
            Category = category,
            Description = description,
            TypicalUse = typicalUse,
            DueDays = dueDays,
            DiscountPercent = discountPercent,
            DiscountDays = discountDays,
            IsDefault = isDefault,
            SortOrder = sortOrder
        };
}
