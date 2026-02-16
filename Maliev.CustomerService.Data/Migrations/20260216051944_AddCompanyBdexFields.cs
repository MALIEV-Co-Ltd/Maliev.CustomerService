using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyBdexFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BdexVerificationDate",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessObjectives",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStatus",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStatusNameTh",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyTypeCode",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullNameTh",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedFromBdex",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationDate",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockSymbol",
                table: "Companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BdexVerificationDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BusinessObjectives",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyStatusNameTh",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyTypeCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FullNameTh",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsVerifiedFromBdex",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StockSymbol",
                table: "Companies");
        }
    }
}
