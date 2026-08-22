using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;

namespace MonoSlice.Modules.Assessments.Persistence;

public sealed class AssessmentsDbContext : DbContext
{
    public const string DefaultSchema = "assessments";

    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<GradingDeadLetter> GradingDeadLetters => Set<GradingDeadLetter>();

    public AssessmentsDbContext(DbContextOptions<AssessmentsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<GradeRecord>(builder =>
        {
            builder.ToTable("grade_records");
            builder.HasKey(g => g.Id);

            builder.Property(g => g.ItemType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(g => g.Score)
                .HasPrecision(5, 2)
                .IsRequired();

            builder.Property(g => g.MaxScore)
                .HasPrecision(5, 2)
                .IsRequired();

            builder.Property(g => g.WeightPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(100.00m)
                .IsRequired();

            builder.Property(g => g.EvaluatedAtUtc)
                .IsRequired();

            builder.HasIndex(g => new { g.StudentId, g.CourseId })
                .HasDatabaseName("idx_grades_student");
        });

        modelBuilder.Entity<Certificate>(builder =>
        {
            builder.ToTable("certificates");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CertificateNumber)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(c => c.CertificateNumber)
                .IsUnique();

            builder.Property(c => c.FinalScore)
                .HasPrecision(5, 2)
                .IsRequired();

            builder.Property(c => c.CertificateHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(c => c.CertificateHash)
                .IsUnique();

            builder.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.RevocationReason)
                .HasMaxLength(500);

            builder.Property(c => c.IssuedAtUtc)
                .IsRequired();

            builder.HasIndex(c => new { c.StudentId, c.CourseId })
                .IsUnique()
                .HasDatabaseName("uq_cert_student_course");
        });

        modelBuilder.Entity<GradingDeadLetter>(builder =>
        {
            builder.ToTable("grading_dead_letters");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.StreamMessageId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(d => d.ErrorMessage)
                .IsRequired();

            builder.Property(d => d.FailedAtUtc)
                .IsRequired();

            builder.Property(d => d.IsResolved)
                .HasDefaultValue(false);
        });

        base.OnModelCreating(modelBuilder);
    }
}
