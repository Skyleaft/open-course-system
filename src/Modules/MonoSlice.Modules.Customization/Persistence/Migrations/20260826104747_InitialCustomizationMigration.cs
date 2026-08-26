using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoSlice.Modules.Customization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCustomizationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customization");

            migrationBuilder.CreateTable(
                name: "landing_sections",
                schema: "customization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    subtitle = table.Column<string>(type: "text", nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_landing_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings_audit_logs",
                schema: "customization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "site_settings",
                schema: "customization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    setting_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_landing_sections_active",
                schema: "customization",
                table: "landing_sections",
                columns: new[] { "is_active", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_settings_audit_key",
                schema: "customization",
                table: "settings_audit_logs",
                column: "setting_key");

            migrationBuilder.CreateIndex(
                name: "idx_settings_category",
                schema: "customization",
                table: "site_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "idx_settings_public",
                schema: "customization",
                table: "site_settings",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "uq_settings_key",
                schema: "customization",
                table: "site_settings",
                column: "setting_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "landing_sections",
                schema: "customization");

            migrationBuilder.DropTable(
                name: "settings_audit_logs",
                schema: "customization");

            migrationBuilder.DropTable(
                name: "site_settings",
                schema: "customization");
        }
    }
}
