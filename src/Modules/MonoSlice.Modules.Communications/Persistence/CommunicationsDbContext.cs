using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Domain;

namespace MonoSlice.Modules.Communications.Persistence;

public sealed class CommunicationsDbContext : DbContext
{
    public const string DefaultSchema = "communications";

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<DiscussionThread> DiscussionThreads => Set<DiscussionThread>();
    public DbSet<ThreadComment> ThreadComments => Set<ThreadComment>();

    public CommunicationsDbContext(DbContextOptions<CommunicationsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<Announcement>(builder =>
        {
            builder.ToTable("announcements");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasColumnName("id");

            builder.Property(a => a.CourseId)
                .HasColumnName("course_id");

            builder.Property(a => a.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(a => a.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(a => a.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(a => a.IsPinned)
                .HasColumnName("is_pinned")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(a => a.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(a => a.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");

            builder.HasIndex(a => a.CourseId)
                .HasDatabaseName("idx_announcements_course");

            // Ignore domain events property from base AggregateRoot
            builder.Ignore(a => a.DomainEvents);
            builder.Ignore(a => a.CreatedAt);
            builder.Ignore(a => a.UpdatedAt);
        });

        modelBuilder.Entity<DiscussionThread>(builder =>
        {
            builder.ToTable("discussion_threads");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id");

            builder.Property(t => t.CourseId)
                .HasColumnName("course_id")
                .IsRequired();

            builder.Property(t => t.LessonId)
                .HasColumnName("lesson_id");

            builder.Property(t => t.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(t => t.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(t => t.IsClosed)
                .HasColumnName("is_closed")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(t => t.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(t => t.ClosedAtUtc)
                .HasColumnName("closed_at_utc");

            builder.Property(t => t.ClosedByUserId)
                .HasColumnName("closed_by_user_id");

            builder.HasIndex(t => t.CourseId)
                .HasDatabaseName("idx_threads_course");

            builder.HasMany(t => t.Comments)
                .WithOne(c => c.Thread)
                .HasForeignKey(c => c.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(t => t.Comments)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(t => t.DomainEvents);
            builder.Ignore(t => t.CreatedAt);
            builder.Ignore(t => t.UpdatedAt);
        });

        modelBuilder.Entity<ThreadComment>(builder =>
        {
            builder.ToTable("thread_comments");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasColumnName("id");

            builder.Property(c => c.ThreadId)
                .HasColumnName("thread_id")
                .IsRequired();

            builder.Property(c => c.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            builder.Property(c => c.ParentCommentId)
                .HasColumnName("parent_comment_id");

            builder.Property(c => c.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(c => c.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(c => c.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");

            builder.HasIndex(c => c.ThreadId)
                .HasDatabaseName("idx_comments_thread");

            builder.HasOne(c => c.ParentComment)
                .WithMany(p => p.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(c => c.CreatedAt);
            builder.Ignore(c => c.UpdatedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}
