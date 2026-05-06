using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maliev.CustomerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPaymentTermReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DueDays",
                table: "PaymentTerms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PaymentTerms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PaymentTerms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DiscountDays",
                table: "PaymentTerms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "PaymentTerms",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypicalUse",
                table: "PaymentTerms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "DUE_ON_RECEIPT",
                columns: new[] { "Category", "Description", "DiscountDays", "DiscountPercent", "TypicalUse" },
                values: new object[] { "Immediate", "Payment is due as soon as the invoice is received.", null, null, "Use for one-off jobs, quick-turnaround work, or customers without approved credit." });

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_15",
                columns: new[] { "Category", "Description", "DiscountDays", "DiscountPercent", "SortOrder", "TypicalUse" },
                values: new object[] { "Net", "Full invoice amount is due 15 calendar days after the invoice date.", null, null, 35, "Use for repeat customers with modest credit exposure or jobs where two-week payment is commercially acceptable." });

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_30",
                columns: new[] { "Category", "Description", "DiscountDays", "DiscountPercent", "SortOrder", "TypicalUse" },
                values: new object[] { "Net", "Full invoice amount is due 30 calendar days after the invoice date.", null, null, 40, "Use as the standard B2B trade-credit term for approved customers with normal purchasing cycles." });

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_45",
                columns: new[] { "Category", "Description", "DiscountDays", "DiscountPercent", "TypicalUse" },
                values: new object[] { "Net", "Full invoice amount is due 45 calendar days after the invoice date.", null, null, "Use for larger commercial customers that require longer internal approval cycles." });

            migrationBuilder.InsertData(
                table: "PaymentTerms",
                columns: new[] { "Code", "Category", "Description", "DiscountDays", "DiscountPercent", "DueDays", "IsDefault", "Name", "SortOrder", "TypicalUse" },
                values: new object[,]
                {
                    { "CASH_ON_DELIVERY", "Delivery", "Payment is collected when goods are delivered.", null, null, 0, false, "Cash on delivery", 15, "Use for local delivery or pickup workflows where collection happens at handoff instead of at invoice issue." },
                    { "CASH_WITH_ORDER", "Advance", "Payment is collected when the order is placed.", null, null, 0, false, "Cash with order", 10, "Use for ecommerce-like orders, small standard items, or orders that should not enter production without payment." },
                    { "END_OF_MONTH", "Calendar", "Payment is due by the last day of the month in which the invoice is issued.", null, null, null, false, "End of month", 60, "Use when the customer pays all invoices in a monthly closing cycle." },
                    { "FIFTEENTH_PROXIMO", "Calendar", "Payment is due on the 15th day of the month after the invoice month.", null, null, null, false, "15th of following month", 75, "Use for customers that run a fixed mid-month payment cycle." },
                    { "FIFTY_DEPOSIT_BALANCE_BEFORE_SHIP", "Deposit", "The customer pays 50% before production and the remaining 50% before shipment.", null, null, null, false, "50% deposit, balance before shipment", 20, "Use for custom manufacturing, special-order materials, tooling, and larger jobs where MALIEV should not carry the full working-capital risk." },
                    { "INSTALLMENTS", "Installment", "Payment is split into scheduled installments instead of one invoice due date.", null, null, null, false, "Installments", 100, "Use for high-value projects with a negotiated payment schedule that should be documented on the quote or contract." },
                    { "MILESTONE_PROGRESS", "Milestone", "Invoices are issued and paid as agreed project milestones are completed.", null, null, null, false, "Milestone / progress payments", 95, "Use for long-running engineering, tooling, fabrication, or project work where billing should follow delivery phases." },
                    { "NET_10", "Net", "Full invoice amount is due 10 calendar days after the invoice date.", null, null, 10, false, "Net 10", 30, "Use for short trade-credit arrangements where the customer needs processing time but not a full month." },
                    { "NET_30_EOM", "Calendar", "Payment is due 30 days after the end of the invoice month.", null, null, null, false, "Net 30 EOM", 65, "Use for customers whose accounts payable process calculates due dates from month-end rather than invoice date." },
                    { "NET_60", "Net", "Full invoice amount is due 60 calendar days after the invoice date.", null, null, 60, false, "Net 60", 50, "Use only for established enterprise or government accounts where longer credit has been approved." },
                    { "NET_60_EOM", "Calendar", "Payment is due 60 days after the end of the invoice month.", null, null, null, false, "Net 60 EOM", 70, "Use for approved enterprise accounts with month-end plus extended credit cycles." },
                    { "NET_7", "Net", "Full invoice amount is due 7 calendar days after the invoice date.", null, null, 7, false, "Net 7", 25, "Use for newer accounts or small businesses where cash collection should stay tight but short trade credit is acceptable." },
                    { "NET_90", "Net", "Full invoice amount is due 90 calendar days after the invoice date.", null, null, 90, false, "Net 90", 55, "Use sparingly for strategic enterprise agreements because it materially increases working-capital exposure." },
                    { "ONE_TEN_NET_30", "Discount", "Customer may deduct 1% if payment is received within 10 days; otherwise the full amount is due in 30 days.", 10, 1m, 30, false, "1/10 Net 30", 80, "Use when a small early-payment incentive helps improve cash flow without heavily discounting margin." },
                    { "PREPAID", "Advance", "Full payment is required before work starts or goods are released.", null, null, 0, false, "Prepaid", 5, "Use for new customers, high-risk accounts, custom materials, or orders that require committed cash before production." },
                    { "TWO_TEN_NET_30", "Discount", "Customer may deduct 2% if payment is received within 10 days; otherwise the full amount is due in 30 days.", 10, 2m, 30, false, "2/10 Net 30", 85, "Use for approved accounts where faster cash collection is worth the discount." },
                    { "TWO_TEN_NET_60", "Discount", "Customer may deduct 2% if payment is received within 10 days; otherwise the full amount is due in 60 days.", 10, 2m, 60, false, "2/10 Net 60", 90, "Use for large accounts that require longer net terms but can be encouraged to pay early." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "CASH_ON_DELIVERY");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "CASH_WITH_ORDER");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "END_OF_MONTH");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "FIFTEENTH_PROXIMO");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "FIFTY_DEPOSIT_BALANCE_BEFORE_SHIP");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "INSTALLMENTS");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "MILESTONE_PROGRESS");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_10");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_30_EOM");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_60");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_60_EOM");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_7");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_90");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "ONE_TEN_NET_30");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "PREPAID");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "TWO_TEN_NET_30");

            migrationBuilder.DeleteData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "TWO_TEN_NET_60");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "DiscountDays",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "TypicalUse",
                table: "PaymentTerms");

            migrationBuilder.AlterColumn<int>(
                name: "DueDays",
                table: "PaymentTerms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_15",
                column: "SortOrder",
                value: 15);

            migrationBuilder.UpdateData(
                table: "PaymentTerms",
                keyColumn: "Code",
                keyValue: "NET_30",
                column: "SortOrder",
                value: 30);
        }
    }
}
