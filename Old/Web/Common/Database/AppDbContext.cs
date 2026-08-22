using CoreLib.BankSoal;
using CoreLib.Report;
using CoreLib.RoomSet;
using CoreLib.TakeExam;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web.Common.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Soal> Soal { get; set; }
    public DbSet<SoalJawaban> SoalJawaban { get; set; }
    public DbSet<Exam> Exam { get; set; }
    public DbSet<Room> Room { get; set; }
    public DbSet<UserExam> UserExam { get; set; }
    public DbSet<UserAnswer> UserAnswer { get; set; }
    public DbSet<Provinsi> Provinsi { get; set; }
    public DbSet<Kota> Kota { get; set; }
    public DbSet<ExamReport> ExamReport { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .Entity<ExamReport>()
            .ToView("vw_examreport")
            .HasKey(x => x.Id);
    }
}