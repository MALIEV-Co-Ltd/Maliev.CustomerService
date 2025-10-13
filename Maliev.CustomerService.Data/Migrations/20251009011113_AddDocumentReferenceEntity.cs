using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReferenceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_document_references", x => x.id);
                    table.CheckConstraint("ck_documentreference_owner_type", "owner_type IN ('Customer', 'Company')");
                    table.CheckConstraint("ck_documentreference_status", "status IN ('Pending', 'Complete', 'PendingDeletion', 'Orphaned', 'MissingFile')");
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_references");
        }
    }
}
