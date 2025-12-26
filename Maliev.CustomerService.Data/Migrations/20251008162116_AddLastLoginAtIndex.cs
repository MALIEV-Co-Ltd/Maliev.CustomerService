using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastLoginAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_aspnetusers_created_at",
                table: "asp_net_users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_aspnetusers_is_active",
                table: "asp_net_users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_aspnetusers_last_login_at",
                table: "asp_net_users",
                column: "last_login_at");

            migrationBuilder.CreateIndex(
                name: "ix_aspnetusers_linked_customer_id",
                table: "asp_net_users",
                column: "linked_customer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_aspnetusers_created_at",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "ix_aspnetusers_is_active",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "ix_aspnetusers_last_login_at",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "ix_aspnetusers_linked_customer_id",
                table: "asp_net_users");
        }
    }
}
