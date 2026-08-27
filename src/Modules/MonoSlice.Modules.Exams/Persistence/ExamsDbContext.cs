using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MonoSlice.Modules.Exams.Domain;

namespace MonoSlice.Modules.Exams.Persistence;

public sealed class ExamsDbContext : DbContext
{
    public const string DefaultSchema = "exams";

    public DbSet<ExamRule> ExamRules => Set<ExamRule>();
    public DbSet<QuizExam> Exams => Set<QuizExam>();
    public DbSet<QuestionBank> QuestionBanks => Set<QuestionBank>();
    public DbSet<BankQuestion> BankQuestions => Set<BankQuestion>();
    public DbSet<QuizSection> Sections => Set<QuizSection>();
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

        // ExamRule
        modelBuilder.Entity<ExamRule>(builder =>
        {
            builder.ToTable("exam_rules", DefaultSchema);
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedNever();

            builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
            builder.Property(r => r.Description);
            builder.Property(r => r.IsSystemPreset).HasDefaultValue(false);
            builder.Property(r => r.CanTabSwitch).HasDefaultValue(false);
            builder.Property(r => r.MaxTabSwitchesAllowed).HasDefaultValue(0);
            builder.Property(r => r.RestrictClipboardAndMouse).HasDefaultValue(true);
            builder.Property(r => r.ForceFullscreen).HasDefaultValue(true);
            builder.Property(r => r.KeyboardDetection).HasDefaultValue(true);
            builder.Property(r => r.RequireCamera).HasDefaultValue(true);
            builder.Property(r => r.SnapshotIntervalSeconds).HasDefaultValue(45);
            builder.Property(r => r.RequireMicrophone).HasDefaultValue(false);
            builder.Property(r => r.MaxAllowedViolations).HasDefaultValue(3);
            builder.Property(r => r.AutoDisqualifyOnExceed).HasDefaultValue(true);
            builder.Property(r => r.CreatedBy);
            builder.Property(r => r.CreatedAtUtc).IsRequired();
            builder.Property(r => r.UpdatedAtUtc);

            builder.HasIndex(r => r.IsSystemPreset);
            builder.HasIndex(r => r.CreatedBy);
        });

        // QuizExam
        modelBuilder.Entity<QuizExam>(builder =>
        {
            builder.ToTable("quiz_exams", DefaultSchema);
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.InstructorId).IsRequired();
            builder.Property(e => e.Title).HasMaxLength(255).IsRequired();
            builder.Property(e => e.Description);

            builder.Property(e => e.ExamRuleId);
            builder.Property(e => e.RuleConfig)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<ExamRuleConfig>(v, jsonSerializerOptions) ?? ExamRuleConfig.StrictProctored(),
                    new ValueComparer<ExamRuleConfig>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<ExamRuleConfig>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.Property(e => e.DurationMinutes).HasDefaultValue(60).IsRequired();
            builder.Property(e => e.PassingScore).HasPrecision(5, 2).HasDefaultValue(70m);
            builder.Property(e => e.MaxAttempts).HasDefaultValue(1);
            builder.Property(e => e.AvailableFromUtc);
            builder.Property(e => e.AvailableToUtc);
            builder.Property(e => e.IsPublished).HasDefaultValue(false);
            builder.Property(e => e.ShuffleQuestions).HasDefaultValue(true);
            builder.Property(e => e.ShuffleOptions).HasDefaultValue(true);
            builder.Property(e => e.CreatedBy).IsRequired();
            builder.Property(e => e.UpdatedBy);
            builder.Property(e => e.CreatedAtUtc).IsRequired();
            builder.Property(e => e.UpdatedAtUtc);

            builder.HasOne<ExamRule>()
                .WithMany()
                .HasForeignKey(e => e.ExamRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.Sections)
                .WithOne()
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.InstructorId);
            builder.HasIndex(e => e.IsPublished);
            builder.HasIndex(e => e.CreatedBy);
            builder.HasIndex(e => e.ExamRuleId);
        });

        // QuestionBank (Package Aggregate)
        modelBuilder.Entity<QuestionBank>(builder =>
        {
            builder.ToTable("question_banks", DefaultSchema);
            builder.HasKey(q => q.Id);
            builder.Property(q => q.Id).ValueGeneratedNever();

            builder.Property(q => q.Title).HasMaxLength(255).IsRequired();
            builder.Property(q => q.Description);
            builder.Property(q => q.Category).HasMaxLength(100);

            // Tags JSONB / Array
            builder.Property(q => q.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, jsonSerializerOptions) ?? new List<string>(),
                    new ValueComparer<List<string>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.Property(q => q.CreatedBy).IsRequired();
            builder.Property(q => q.UpdatedBy);
            builder.Property(q => q.CreatedAtUtc).IsRequired();
            builder.Property(q => q.UpdatedAtUtc);

            builder.HasMany(q => q.Questions)
                .WithOne()
                .HasForeignKey(bq => bq.BankId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(q => q.CreatedBy);
            builder.HasIndex(q => q.Category);
        });

        // BankQuestion
        modelBuilder.Entity<BankQuestion>(builder =>
        {
            builder.ToTable("bank_questions", DefaultSchema);
            builder.HasKey(bq => bq.Id);
            builder.Property(bq => bq.Id).ValueGeneratedNever();

            builder.Property(bq => bq.BankId).IsRequired();
            builder.Property(bq => bq.QuestionText).IsRequired();
            builder.Property(bq => bq.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(bq => bq.GradingMethod)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(GradingMethod.PartialWithPenalty)
                .IsRequired();

            builder.Property(bq => bq.Points).HasPrecision(5, 2).HasDefaultValue(1m);
            builder.Property(bq => bq.OrderIndex).HasDefaultValue(1).IsRequired();
            builder.Property(bq => bq.Explanation);

            // Options JSONB
            builder.Property(bq => bq.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<List<QuestionOption>>(v, jsonSerializerOptions) ?? new List<QuestionOption>(),
                    new ValueComparer<List<QuestionOption>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<List<QuestionOption>>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

            builder.HasIndex(bq => bq.BankId);
        });

        // QuizSection
        modelBuilder.Entity<QuizSection>(builder =>
        {
            builder.ToTable("quiz_sections", DefaultSchema);
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.ExamId).IsRequired();
            builder.Property(s => s.QuestionBankId).IsRequired();
            builder.Property(s => s.Title).HasMaxLength(255).IsRequired();
            builder.Property(s => s.Description);
            builder.Property(s => s.OrderIndex).HasDefaultValue(1).IsRequired();
            builder.Property(s => s.PointsOverride).HasPrecision(5, 2);
            builder.Property(s => s.QuestionCount);

            builder.HasOne(s => s.QuestionBank)
                .WithMany()
                .HasForeignKey(s => s.QuestionBankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.ExamId);
            builder.HasIndex(s => s.QuestionBankId);
        });

        // QuizSubmission
        modelBuilder.Entity<QuizSubmission>(builder =>
        {
            builder.ToTable("quiz_submissions", DefaultSchema);
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.ExamId).IsRequired();
            builder.Property(s => s.StudentId).IsRequired();

            builder.Property(s => s.AppliedRules)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonSerializerOptions),
                    v => JsonSerializer.Deserialize<ExamRuleConfig>(v, jsonSerializerOptions) ?? ExamRuleConfig.StrictProctored(),
                    new ValueComparer<ExamRuleConfig>(
                        (c1, c2) => JsonSerializer.Serialize(c1, jsonSerializerOptions) == JsonSerializer.Serialize(c2, jsonSerializerOptions),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, jsonSerializerOptions).GetHashCode(),
                        c => JsonSerializer.Deserialize<ExamRuleConfig>(JsonSerializer.Serialize(c, jsonSerializerOptions), jsonSerializerOptions)!
                    )
                );

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
