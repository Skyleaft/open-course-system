using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoSlice.Modules.Exams.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamRulesAndConfigurableProctoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAllowedViolations",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.DropColumn(
                name: "Mode",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.AddColumn<string>(
                name: "AppliedRules",
                schema: "exams",
                table: "quiz_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ExamRuleId",
                schema: "exams",
                table: "quiz_exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleConfig",
                schema: "exams",
                table: "quiz_exams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "exam_rules",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsSystemPreset = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CanTabSwitch = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MaxTabSwitchesAllowed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RestrictClipboardAndMouse = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ForceFullscreen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    KeyboardDetection = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequireCamera = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SnapshotIntervalSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 45),
                    RequireMicrophone = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MaxAllowedViolations = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    AutoDisqualifyOnExceed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quiz_exams_ExamRuleId",
                schema: "exams",
                table: "quiz_exams",
                column: "ExamRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_rules_CreatedBy",
                schema: "exams",
                table: "exam_rules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_exam_rules_IsSystemPreset",
                schema: "exams",
                table: "exam_rules",
                column: "IsSystemPreset");

            migrationBuilder.AddForeignKey(
                name: "FK_quiz_exams_exam_rules_ExamRuleId",
                schema: "exams",
                table: "quiz_exams",
                column: "ExamRuleId",
                principalSchema: "exams",
                principalTable: "exam_rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quiz_exams_exam_rules_ExamRuleId",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.DropTable(
                name: "exam_rules",
                schema: "exams");

            migrationBuilder.DropIndex(
                name: "IX_quiz_exams_ExamRuleId",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.DropColumn(
                name: "AppliedRules",
                schema: "exams",
                table: "quiz_submissions");

            migrationBuilder.DropColumn(
                name: "ExamRuleId",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.DropColumn(
                name: "RuleConfig",
                schema: "exams",
                table: "quiz_exams");

            migrationBuilder.AddColumn<int>(
                name: "MaxAllowedViolations",
                schema: "exams",
                table: "quiz_exams",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                schema: "exams",
                table: "quiz_exams",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
