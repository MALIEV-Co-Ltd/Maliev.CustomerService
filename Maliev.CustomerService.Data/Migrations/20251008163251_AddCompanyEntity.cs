using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    vat_number = table.Column<string>(type: "text", nullable: true),
                    registration_number = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    segment = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_companies", x => x.id);
                    table.CheckConstraint("ck_company_segment", "segment IN ('Retail', 'Wholesale', 'Enterprise', 'Government')");
                    table.CheckConstraint("ck_company_tier", "tier IN ('Bronze', 'Silver', 'Gold', 'Platinum', 'VIP')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_name",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_company_segment",
                table: "companies",
                column: "segment");

            migrationBuilder.CreateIndex(
                name: "ix_company_tier",
                table: "companies",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "ix_company_vat_number",
                table: "companies",
                column: "vat_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
