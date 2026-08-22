using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MonoSlice.Modules.Exams.Domain;

namespace MonoSlice.Modules.Exams.Persistence;

public sealed class ExamsDbContext : DbContext
{
    public const string DefaultSchema = "exams";

    public DbSet<QuizExam> Exams => Set<QuizExam>();
    public DbSet<QuizQuestion> Questions => Set<QuizQuestion>();
    public DbSet<QuizSubmission> Submissions => Set<QuizSubmission>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<ProctoringSnapshot> Snapshots => Set<ProctoringSnapshot>();

    public ExamsDbContext(DbContextOptions<ExamsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        var jsonSerializerOptions = new JsonSerializerOptions();

        // QuizExam
        modelBuilder.Entity<QuizExam>(builder =>
        {
            builder.ToTable("quiz_exams", DefaultSchema);
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.InstructorId).IsRequired();
            builder.Property(e => e.Title).HasMaxLength(255).IsRequired();
            builder.Property(e => e.Description);
            builder.Property(e => e.Mode)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.DurationMinutes).HasDefaultValue(60).IsRequired();
            builder.Property(e => e.PassingScore).HasPrecision(5, 2).HasDefaultValue(70m);
            builder.Property(e => e.MaxAllowedViolations).HasDefaultValue(3);
            builder.Property(e => e.IsPublished).HasDefaultValue(false);
            builder.Property(e => e.ShuffleQuestions).HasDefaultValue(true);
            builder.Property(e => e.ShuffleOptions).HasDefaultValue(true);
            builder.Property(e => e.CreatedAtUtc).IsRequired();
            builder.Property(e => e.UpdatedAtUtc);

            builder.HasMany(e => e.Questions)
                .WithOne()
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.InstructorId);
            builder.HasIndex(e => e.CourseId);
            builder.HasIndex(e => e.IsPublished);
        });

        // QuizQuestion
        modelBuilder.Entity<QuizQuestion>(builder =>
        {
            builder.ToTable("quiz_questions", DefaultSchema);
            builder.HasKey(q => q.Id);
            builder.Property(q => q.Id).ValueGeneratedNever();

            builder.Property(q => q.QuestionText).IsRequired();
            builder.Property(q => q.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(q => q.Points).HasPrecision(5, 2).HasDefaultValue(1m);
            builder.Property(q => q.OrderIndex).IsRequired();
            builder.Property(q => q.Explanation);

            // Options JSONB
            builder.Property(q => q.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<QuestionOption>>(v, jsonSerializerOptions) ?? new List<QuestionOption>(),
                    new ValueComparer<List<QuestionOption>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<List<QuestionOption>>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.HasIndex(q => q.ExamId);
        });

        // QuizSubmission
        modelBuilder.Entity<QuizSubmission>(builder =>
        {
            builder.ToTable("quiz_submissions", DefaultSchema);
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.ExamId).IsRequired();
            builder.Property(s => s.StudentId).IsRequired();
            builder.Property(s => s.StartedAtUtc).IsRequired();
            builder.Property(s => s.MaxAllowedEndTimeUtc).IsRequired();
            builder.Property(s => s.SubmittedAtUtc);
            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.RandomSeed).IsRequired();
            builder.Property(s => s.ActiveSessionToken).HasMaxLength(255).IsRequired();
            builder.Property(s => s.Score).HasPrecision(5, 2);
            builder.Property(s => s.IsPassed);

            // Violations JSONB
            builder.Property(s => s.Violations)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<ViolationRecord>>(v, jsonSerializerOptions) ?? new List<ViolationRecord>(),
                    new ValueComparer<List<ViolationRecord>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<List<ViolationRecord>>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.HasMany(s => s.Answers)
                .WithOne()
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Snapshots)
                .WithOne()
                .HasForeignKey(sp => sp.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.ExamId, s.StudentId });
            builder.HasIndex(s => s.StudentId);
            builder.HasIndex(s => s.Status);
        });

        // StudentAnswer
        modelBuilder.Entity<StudentAnswer>(builder =>
        {
            builder.ToTable("student_answers", DefaultSchema);
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();

            builder.Property(a => a.QuestionId).IsRequired();
            builder.Property(a => a.SelectedOptionIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, jsonSerializerOptions) ?? new List<Guid>(),
                    new ValueComparer<List<Guid>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<List<Guid>>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.Property(a => a.EssayText);
            builder.Property(a => a.AwardedScore).HasPrecision(5, 2);
            builder.Property(a => a.AnsweredAtUtc).IsRequired();

            builder.HasIndex(a => new { a.SubmissionId, a.QuestionId }).IsUnique();
        });

        // ProctoringSnapshot
        modelBuilder.Entity<ProctoringSnapshot>(builder =>
        {
            builder.ToTable("proctoring_snapshots", DefaultSchema);
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.StorageKey).HasMaxLength(1000).IsRequired();
            builder.Property(p => p.CapturedAtUtc).IsRequired();

            builder.HasIndex(p => p.SubmissionId);
        });
    }
}
