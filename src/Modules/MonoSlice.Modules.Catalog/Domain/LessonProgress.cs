using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class LessonProgress : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid LessonId { get; private set; }
    public bool IsCompleted { get; private set; } = true;
    public DateTime? CompletedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime LastAccessedAtUtc { get; private set; } = DateTime.UtcNow;

    private LessonProgress() : base(Guid.CreateVersion7()) { }

    public static LessonProgress Create(Guid userId, Guid courseId, Guid lessonId, bool isCompleted = true)
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException("User ID is required for lesson progress.");
        }

        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required for lesson progress.");
        }

        if (lessonId == Guid.Empty)
        {
            throw new ValidationException("Lesson ID is required for lesson progress.");
        }

        return new LessonProgress
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CourseId = courseId,
            LessonId = lessonId,
            IsCompleted = isCompleted,
            CompletedAtUtc = isCompleted ? DateTime.UtcNow : null,
            LastAccessedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        CompletedAtUtc = DateTime.UtcNow;
        LastAccessedAtUtc = DateTime.UtcNow;
    }

    public void MarkUncompleted()
    {
        IsCompleted = false;
        CompletedAtUtc = null;
        LastAccessedAtUtc = DateTime.UtcNow;
    }

    public void RecordAccess()
    {
        LastAccessedAtUtc = DateTime.UtcNow;
    }
}
