using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Migrations
{
    /// <inheritdoc />
    public partial class report_view_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE OR REPLACE VIEW public.VW_ExamReport AS
 SELECT ""UserExam"".""Id"",
    ""AspNetUsers"".""NamaLengkap"",
    ""AspNetUsers"".""Gender"",
    ""Kota"".""NamaKota"" AS ""AsalKota"",
    ""AspNetUsers"".""PhoneNumber"" AS ""NomorTelepon"",
    ""Room"".""Nama"" AS ""NamaRoom"",
    ""Room"".""JadwalStart"" AS ""Jadwal"",
    sum(sj.""Point"") AS ""Score"",
    sum(soalpoint.""maxPoint"") AS ""MaxScore"",
    (sum(sj.""Point"")::numeric(7,2) / sum(soalpoint.""maxPoint"")::numeric(7,2) * 100::numeric)::numeric(7,2) AS nilai
   FROM ""SoalJawaban"" sj
     JOIN ""UserAnswer"" ON sj.""Id"" = ""UserAnswer"".""SoalJawabanId""
     JOIN ""UserExam"" ON ""UserAnswer"".""UserExamId"" = ""UserExam"".""Id""
     JOIN ""AspNetUsers"" ON ""UserExam"".""UserId""::text = ""AspNetUsers"".""Id""
     JOIN ""Soal"" ON sj.""SoalId"" = ""Soal"".""Id"" AND ""UserAnswer"".""SoalId"" = ""Soal"".""Id""
     JOIN ( SELECT s.""Id"",
            s.""Pertanyaan"",
            max(sj_1.""Point"") AS ""maxPoint"",
            min(sj_1.""Point"") AS ""minPoint""
           FROM ""Soal"" s
             JOIN ""SoalJawaban"" sj_1 ON s.""Id"" = sj_1.""SoalId""
          GROUP BY s.""Id""
          ORDER BY s.""ExamId"") soalpoint ON ""Soal"".""Id"" = soalpoint.""Id""
     LEFT JOIN reference.""Kota"" ON ""AspNetUsers"".""KotaId""::text = ""Kota"".""Id""::text
     JOIN ""Room"" ON ""UserExam"".""RoomId"" = ""Room"".""Id""
  GROUP BY ""UserExam"".""Id"", ""AspNetUsers"".""NamaLengkap"", ""AspNetUsers"".""Gender"", ""AspNetUsers"".""PhoneNumber"", ""Kota"".""NamaKota"", ""Room"".""Nama"", ""Room"".""JadwalStart""
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW public.VW_ExamReport;");
        }
    }
}
