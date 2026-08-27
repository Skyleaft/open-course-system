using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoSlice.Modules.Exams.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingMethodToBankQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GradingMethod",
                schema: "exams",
                table: "bank_questions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PartialWithPenalty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradingMethod",
                schema: "exams",
                table: "bank_questions");
        }
    }
}
