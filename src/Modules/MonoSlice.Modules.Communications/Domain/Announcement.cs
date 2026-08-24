using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Communications.Domain;

public sealed class Announcement : AggregateRoot<Guid>
{
    public Guid? CourseId { get; private set; } // NULL = Global Platform Announcement
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsPinned { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Announcement() { }

    private Announcement(
        Guid id,
        Guid? courseId,
        Guid authorId,
        string title,
        string content,
        bool isPinned,
        DateTime createdAtUtc) : base(id)
    {
        CourseId = courseId;
        AuthorId = authorId;
        Title = title;
        Content = content;
        IsPinned = isPinned;
        CreatedAtUtc = createdAtUtc;
    }

    public static Announcement Create(
        Guid? courseId,
        Guid authorId,
        string title,
        string content,
        bool isPinned = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Announcement(
            Guid.CreateVersion7(),
            courseId,
            authorId,
            title.Trim(),
            content.Trim(),
            isPinned,
            DateTime.UtcNow);
    }

    public void Update(string title, string content, bool isPinned)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Title = title.Trim();
        Content = content.Trim();
        IsPinned = isPinned;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Pin()
    {
        IsPinned = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unpin()
    {
        IsPinned = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
