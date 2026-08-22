using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Communications.Domain;

public sealed class DiscussionThread : AggregateRoot<Guid>
{
    private readonly List<ThreadComment> _comments = [];

    public Guid CourseId { get; private set; }
    public Guid? LessonId { get; private set; } // NULL = Course-level General Thread
    public Guid AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsClosed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    public IReadOnlyCollection<ThreadComment> Comments => _comments.AsReadOnly();

    private DiscussionThread() { }

    private DiscussionThread(
        Guid id,
        Guid courseId,
        Guid? lessonId,
        Guid authorId,
        string title,
        string content,
        DateTime createdAtUtc) : base(id)
    {
        CourseId = courseId;
        LessonId = lessonId;
        AuthorId = authorId;
        Title = title;
        Content = content;
        IsClosed = false;
        CreatedAtUtc = createdAtUtc;
    }

    public static DiscussionThread Create(
        Guid courseId,
        Guid? lessonId,
        Guid authorId,
        string title,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new DiscussionThread(
            Guid.CreateVersion7(),
            courseId,
            lessonId,
            authorId,
            title.Trim(),
            content.Trim(),
            DateTime.UtcNow);
    }

    public void Close(Guid closedByUserId)
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        ClosedAtUtc = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
    }

    public void Reopen()
    {
        IsClosed = false;
        ClosedAtUtc = null;
        ClosedByUserId = null;
    }

    public ThreadComment AddComment(Guid authorId, string content, Guid? parentCommentId = null)
    {
        if (IsClosed)
        {
            throw new BusinessRuleException("Cannot add comment to a closed discussion thread.");
        }

        var comment = ThreadComment.Create(Id, authorId, content, parentCommentId);
        _comments.Add(comment);
        return comment;
    }
}
