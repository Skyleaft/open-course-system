using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoSlice.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTextContentAndNullableContentUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ContentUrl",
                schema: "courses",
                table: "lessons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "TextContent",
                schema: "courses",
                table: "lessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextContent",
                schema: "courses",
                table: "lessons");

            migrationBuilder.AlterColumn<string>(
                name: "ContentUrl",
                schema: "courses",
                table: "lessons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
