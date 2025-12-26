using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNDARecordEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "n_d_a_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    signed_by = table.Column<string>(type: "text", nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_n_d_a_records", x => x.id);
                    table.CheckConstraint("ck_ndarecord_status", "status IN ('Draft', 'Signed', 'Expired', 'Revoked')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_customer_id",
                table: "n_d_a_records",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_expires_at",
                table: "n_d_a_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_status",
                table: "n_d_a_records",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "n_d_a_records");
        }
    }
}
