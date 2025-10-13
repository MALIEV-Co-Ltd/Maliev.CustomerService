using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CustomerService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalNoteEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_internal_notes", x => x.id);
                    table.CheckConstraint("ck_internalnote_owner_type", "owner_type IN ('Customer', 'Company')");
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "internal_notes");
        }
    }
}
