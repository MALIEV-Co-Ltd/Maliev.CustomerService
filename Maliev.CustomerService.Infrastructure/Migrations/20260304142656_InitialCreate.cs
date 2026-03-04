using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    AddressLine1 = table.Column<string>(type: "text", nullable: false),
                    AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    AddressLine3 = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: false),
                    StateProvince = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: true),
                    RecipientPhone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.CheckConstraint("ck_address_owner_type", "\"OwnerType\" IN ('Customer', 'Company')");
                    table.CheckConstraint("ck_address_type", "\"Type\" IN ('Billing', 'Shipping')");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedFields = table.Column<string>(type: "text", nullable: true),
                    PreviousValues = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Segment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentYearPurchaseValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentYearOrderCount = table.Column<int>(type: "integer", nullable: false),
                    TierCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FullNameTh = table.Column<string>(type: "text", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyStatus = table.Column<string>(type: "text", nullable: true),
                    CompanyStatusNameTh = table.Column<string>(type: "text", nullable: true),
                    CompanyTypeCode = table.Column<string>(type: "text", nullable: true),
                    BusinessObjectives = table.Column<string>(type: "text", nullable: true),
                    IsVerifiedFromBdex = table.Column<bool>(type: "boolean", nullable: false),
                    BdexVerificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StockSymbol = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyTierSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TierName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinPurchaseValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinOrderCount = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FreeShippingMinOrder = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CoinRewardPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTierSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Landline = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Segment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommunicationPreferences = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsesCompanyBillingAddress = table.Column<bool>(type: "boolean", nullable: false),
                    ThaiNationalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_references",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    signed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_references", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "internal_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "NDARecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SignedBy = table.Column<string>(type: "text", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokeReason = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDARecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "internal_note_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    internal_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_by_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_note_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_internal_note_comments_internal_notes_internal_note_id",
                        column: x => x.internal_note_id,
                        principalTable: "internal_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_country_id",
                table: "Addresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "ix_address_owner_type_owner_id",
                table: "Addresses",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Tier",
                table: "Companies",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VatNumber",
                table: "Companies",
                column: "VatNumber",
                unique: true,
                filter: "\"VatNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocuments_CompanyId",
                table: "CompanyDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTierSettings_TierName",
                table: "CompanyTierSettings",
                column: "TierName");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTierSettings_TierName_ValidFrom",
                table: "CompanyTierSettings",
                columns: new[] { "TierName", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PrincipalId",
                table: "Customers",
                column: "PrincipalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_references_owner_type_owner_id",
                table: "document_references",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_note_comments_internal_note_id",
                table: "internal_note_comments",
                column: "internal_note_id");

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_owner_type_owner_id",
                table: "internal_notes",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_NDARecords_CustomerId",
                table: "NDARecords",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CompanyDocuments");

            migrationBuilder.DropTable(
                name: "CompanyTierSettings");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "document_references");

            migrationBuilder.DropTable(
                name: "internal_note_comments");

            migrationBuilder.DropTable(
                name: "NDARecords");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "internal_notes");
        }
    }
}
