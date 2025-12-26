using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalizePrincipalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add PrincipalId column to customers table (T001, T027)
            // Note: In an ideal flow, this would be nullable first, then backfilled, then set to nullable: false.
            // For this cleanup, we are enforcing the final state.
            migrationBuilder.AddColumn<Guid>(
                name: "principal_id",
                table: "customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // 2. Add unique lookup index with filter to allow multiple Guid.Empty (T023, T028)
            migrationBuilder.CreateIndex(
                name: "ix_customer_principal_lookup",
                table: "customers",
                column: "principal_id",
                unique: true,
                filter: "is_deleted = false AND principal_id != '00000000-0000-0000-0000-000000000000'");

            // 3. Drop legacy Identity tables (T033)
            // IMPORTANT: Ensure a persistent archive/backup of these tables has been created (T032)
            // before applying this migration in production.
            migrationBuilder.DropTable(name: "asp_net_role_claims");
            migrationBuilder.DropTable(name: "asp_net_user_claims");
            migrationBuilder.DropTable(name: "asp_net_user_logins");
            migrationBuilder.DropTable(name: "asp_net_user_roles");
            migrationBuilder.DropTable(name: "asp_net_user_tokens");
            migrationBuilder.DropTable(name: "asp_net_roles");
            migrationBuilder.DropTable(name: "asp_net_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customer_principal_lookup",
                table: "customers");

            migrationBuilder.CreateIndex(
                name: "ix_customer_principal_lookup",
                table: "customers",
                column: "principal_id",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
