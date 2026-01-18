using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    address_line1 = table.Column<string>(type: "text", nullable: false),
                    address_line2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    province = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "text", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                    table.CheckConstraint("ck_address_owner_type", "owner_type IN ('Customer', 'Company')");
                    table.CheckConstraint("ck_address_type", "type IN ('Billing', 'Shipping')");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    actor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_fields = table.Column<string>(type: "text", nullable: true),
                    previous_values = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.CheckConstraint("ck_company_segment", "segment IN ('Retail', 'Wholesale', 'Enterprise', 'Government')");
                    table.CheckConstraint("ck_company_tier", "tier IN ('Bronze', 'Silver', 'Gold', 'Platinum', 'VIP')");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    segment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    communication_preferences = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.CheckConstraint("ck_customer_segment", "segment IN ('Retail', 'Wholesale', 'Enterprise', 'Government')");
                    table.CheckConstraint("ck_customer_tier", "tier IN ('Bronze', 'Silver', 'Gold', 'Platinum', 'VIP')");
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_references", x => x.id);
                    table.CheckConstraint("ck_documentreference_owner_type", "owner_type IN ('Customer', 'Company')");
                    table.CheckConstraint("ck_documentreference_status", "status IN ('Pending', 'Complete', 'PendingDeletion', 'Orphaned', 'MissingFile')");
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_notes", x => x.id);
                    table.CheckConstraint("ck_internalnote_owner_type", "owner_type IN ('Customer', 'Company')");
                });

            migrationBuilder.CreateTable(
                name: "nda_records",
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000001'::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nda_records", x => x.id);
                    table.CheckConstraint("ck_ndarecord_status", "status IN ('Draft', 'Signed', 'Expired', 'Revoked')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_country_id",
                table: "addresses",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_address_owner_type_owner_id",
                table: "addresses",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_actor_id",
                table: "audit_logs",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_actor_type",
                table: "audit_logs",
                column: "actor_type");

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditlog_timestamp",
                table: "audit_logs",
                column: "timestamp");

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

            migrationBuilder.CreateIndex(
                name: "ix_customer_company_id",
                table: "customers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_created_at",
                table: "customers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_customer_email_unique_active",
                table: "customers",
                column: "email",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customer_preferred_language",
                table: "customers",
                column: "preferred_language");

            migrationBuilder.CreateIndex(
                name: "ix_customer_principal_lookup",
                table: "customers",
                column: "principal_id",
                unique: true,
                filter: "is_deleted = false AND principal_id != '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "ix_customer_segment",
                table: "customers",
                column: "segment");

            migrationBuilder.CreateIndex(
                name: "ix_customer_tier",
                table: "customers",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "ix_documentreference_document_type",
                table: "document_references",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "ix_documentreference_owner_type_owner_id",
                table: "document_references",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documentreference_status",
                table: "document_references",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_internalnote_created_at",
                table: "internal_notes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_internalnote_created_by",
                table: "internal_notes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_internalnote_owner_type_owner_id",
                table: "internal_notes",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_customer_id",
                table: "nda_records",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_expires_at",
                table: "nda_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_ndarecord_status",
                table: "nda_records",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "document_references");

            migrationBuilder.DropTable(
                name: "internal_notes");

            migrationBuilder.DropTable(
                name: "nda_records");
        }
    }
}
