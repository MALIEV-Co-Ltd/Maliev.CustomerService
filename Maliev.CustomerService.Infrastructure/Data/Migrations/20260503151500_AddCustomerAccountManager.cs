using System;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CustomerDbContext))]
    [Migration("20260503151500_AddCustomerAccountManager")]
    public partial class AddCustomerAccountManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountManagerEmployeeId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_account_manager_employee_id",
                table: "Customers",
                column: "AccountManagerEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_account_manager_employee_id",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AccountManagerEmployeeId",
                table: "Customers");
        }
    }
}
