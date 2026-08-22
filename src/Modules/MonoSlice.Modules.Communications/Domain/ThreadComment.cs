using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Communications.Domain;

public sealed class ThreadComment : Entity<Guid>
{
    public Guid ThreadId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // EF Navigation Properties
    public DiscussionThread? Thread { get; private set; }
    public ThreadComment? ParentComment { get; private set; }
    public ICollection<ThreadComment> Replies { get; private set; } = new List<ThreadComment>();

    private ThreadComment() { }

    private ThreadComment(
        Guid id,
        Guid threadId,
        Guid authorId,
        Guid? parentCommentId,
        string content,
        DateTime createdAtUtc) : base(id)
    {
        ThreadId = threadId;
        AuthorId = authorId;
        ParentCommentId = parentCommentId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
    }

    public static ThreadComment Create(
        Guid threadId,
        Guid authorId,
        string content,
        Guid? parentCommentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new ThreadComment(
            Guid.CreateVersion7(),
            threadId,
            authorId,
            parentCommentId,
            content.Trim(),
            DateTime.UtcNow);
    }

    public void UpdateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Content = content.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
