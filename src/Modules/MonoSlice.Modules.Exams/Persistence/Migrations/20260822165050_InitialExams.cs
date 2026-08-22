using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoSlice.Modules.Exams.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialExams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "exams");

            migrationBuilder.CreateTable(
                name: "quiz_exams",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    PassingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 70m),
                    MaxAllowedViolations = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    AvailableFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShuffleQuestions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShuffleOptions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_exams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_submissions",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxAllowedEndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RandomSeed = table.Column<int>(type: "integer", nullable: false),
                    ActiveSessionToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: true),
                    Violations = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_submissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_questions",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Points = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiz_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quiz_questions_quiz_exams_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "exams",
                        principalTable: "quiz_exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proctoring_snapshots",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctoring_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proctoring_snapshots_quiz_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "exams",
                        principalTable: "quiz_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_answers",
                schema: "exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionIds = table.Column<string>(type: "text", nullable: false),
                    EssayText = table.Column<string>(type: "text", nullable: true),
                    AwardedScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AnsweredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_answers_quiz_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "exams",
                        principalTable: "quiz_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_snapshots_SubmissionId",
                schema: "exams",
                table: "proctoring_snapshots",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_exams_CourseId",
                schema: "exams",
                table: "quiz_exams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_exams_InstructorId",
                schema: "exams",
                table: "quiz_exams",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_exams_IsPublished",
                schema: "exams",
                table: "quiz_exams",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_questions_ExamId",
                schema: "exams",
                table: "quiz_questions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_submissions_ExamId_StudentId",
                schema: "exams",
                table: "quiz_submissions",
                columns: new[] { "ExamId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_quiz_submissions_Status",
                schema: "exams",
                table: "quiz_submissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_submissions_StudentId",
                schema: "exams",
                table: "quiz_submissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_answers_SubmissionId_QuestionId",
                schema: "exams",
                table: "student_answers",
                columns: new[] { "SubmissionId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proctoring_snapshots",
                schema: "exams");

            migrationBuilder.DropTable(
                name: "quiz_questions",
                schema: "exams");

            migrationBuilder.DropTable(
                name: "student_answers",
                schema: "exams");

            migrationBuilder.DropTable(
                name: "quiz_exams",
                schema: "exams");

            migrationBuilder.DropTable(
                name: "quiz_submissions",
                schema: "exams");
        }
    }
}
