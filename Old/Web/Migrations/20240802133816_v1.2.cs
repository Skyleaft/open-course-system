using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Migrations
{
    /// <inheritdoc />
    public partial class v12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScoreData",
                table: "UserExam",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ScoreNormalizeData",
                table: "UserExam",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomExam",
                columns: table => new
                {
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_RoomExam_Room_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Room",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomExam_RoomId",
                table: "RoomExam",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomExam");

            migrationBuilder.DropColumn(
                name: "ScoreData",
                table: "UserExam");

            migrationBuilder.DropColumn(
                name: "ScoreNormalizeData",
                table: "UserExam");
        }
    }
}
