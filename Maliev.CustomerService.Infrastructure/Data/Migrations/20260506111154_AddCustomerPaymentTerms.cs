using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maliev.CustomerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPaymentTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Due on receipt");

            migrationBuilder.CreateTable(
                name: "PaymentTerms",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DueDays = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTerms", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "PaymentTerms",
                columns: new[] { "Code", "DueDays", "IsDefault", "Name", "SortOrder" },
                values: new object[,]
                {
                    { "DUE_ON_RECEIPT", 0, true, "Due on receipt", 0 },
                    { "NET_15", 15, false, "Net 15", 15 },
                    { "NET_30", 30, false, "Net 30", 30 },
                    { "NET_45", 45, false, "Net 45", 45 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Customers");
        }
    }
}
