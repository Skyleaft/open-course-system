using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;

namespace MonoSlice.Modules.Catalog.Persistence;

public sealed class CoursesDbContext : DbContext
{
    public const string DefaultSchema = "courses";

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> Sections => Set<CourseSection>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> Submissions => Set<AssignmentSubmission>();
    public DbSet<CourseEnrollment> Enrollments => Set<CourseEnrollment>();

    public CoursesDbContext(DbContextOptions<CoursesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Course
        modelBuilder.Entity<Course>(builder =>
        {
            builder.ToTable("courses", DefaultSchema);
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();

            builder.Property(c => c.InstructorId).IsRequired();
            builder.Property(c => c.Title).HasMaxLength(255).IsRequired();
            builder.Property(c => c.Description);
            builder.Property(c => c.ThumbnailUrl).HasMaxLength(1000);
            builder.Property(c => c.AccessType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Price).HasPrecision(12, 2).HasDefaultValue(0m);
            builder.Property(c => c.EnrollmentKeyHash).HasMaxLength(255);
            builder.Property(c => c.IsPublished).HasDefaultValue(false);
            builder.Property(c => c.CreatedAtUtc).IsRequired();
            builder.Property(c => c.UpdatedAtUtc);

            builder.HasMany(c => c.Sections)
                .WithOne()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Assignments)
                .WithOne()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.InstructorId);
            builder.HasIndex(c => c.IsPublished);
        });

        // CourseSection
        modelBuilder.Entity<CourseSection>(builder =>
        {
            builder.ToTable("sections", DefaultSchema);
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.Title).HasMaxLength(255).IsRequired();
            builder.Property(s => s.OrderIndex).IsRequired();

            builder.HasMany(s => s.Lessons)
                .WithOne()
                .HasForeignKey(l => l.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => s.CourseId);
        });

        // Lesson
        modelBuilder.Entity<Lesson>(builder =>
        {
            builder.ToTable("lessons", DefaultSchema);
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).ValueGeneratedNever();

            builder.Property(l => l.Title).HasMaxLength(255).IsRequired();
            builder.Property(l => l.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(l => l.ContentUrl).HasMaxLength(1000).IsRequired();
            builder.Property(l => l.DurationMinutes).HasDefaultValue(0);
            builder.Property(l => l.OrderIndex).IsRequired();
            builder.Property(l => l.CreatedAtUtc).IsRequired();

            builder.HasIndex(l => l.SectionId);
        });

        // Assignment
        modelBuilder.Entity<Assignment>(builder =>
        {
            builder.ToTable("assignments", DefaultSchema);
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();

            builder.Property(a => a.Title).HasMaxLength(255).IsRequired();
            builder.Property(a => a.Instruction).IsRequired();
            builder.Property(a => a.DeadlineUtc).IsRequired();
            builder.Property(a => a.MaxScore).HasPrecision(5, 2).HasDefaultValue(100m);
            builder.Property(a => a.CreatedAtUtc).IsRequired();

            builder.HasIndex(a => a.CourseId);
        });

        // AssignmentSubmission
        modelBuilder.Entity<AssignmentSubmission>(builder =>
        {
            builder.ToTable("assignment_submissions", DefaultSchema);
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.AssignmentId).IsRequired();
            builder.Property(s => s.StudentId).IsRequired();
            builder.Property(s => s.FileUrl).HasMaxLength(1000).IsRequired();
            builder.Property(s => s.SubmittedAtUtc).IsRequired();
            builder.Property(s => s.Score).HasPrecision(5, 2);
            builder.Property(s => s.Feedback);
            builder.Property(s => s.GradedAtUtc);

            builder.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
        });

        // CourseEnrollment
        modelBuilder.Entity<CourseEnrollment>(builder =>
        {
            builder.ToTable("enrollments", DefaultSchema);
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.UserId).IsRequired();
            builder.Property(e => e.CourseId).IsRequired();
            builder.Property(e => e.EnrolledAtUtc).IsRequired();

            builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            builder.HasIndex(e => e.UserId);
            builder.HasIndex(e => e.CourseId);
        });
    }
}
