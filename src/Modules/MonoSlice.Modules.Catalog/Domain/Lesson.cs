using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class Lesson : Entity<Guid>
{
    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public LessonType Type { get; private set; } = LessonType.Video;
    public string ContentUrl { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public int OrderIndex { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Lesson() : base(Guid.CreateVersion7()) { }

    public static Lesson Create(
        Guid sectionId,
        string title,
        LessonType type,
        string contentUrl,
        int durationMinutes,
        int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Lesson title cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            throw new ValidationException("Lesson content storage URL/key cannot be empty.");
        }

        return new Lesson
        {
            Id = Guid.CreateVersion7(),
            SectionId = sectionId,
            Title = title.Trim(),
            Type = type,
            ContentUrl = contentUrl.Trim(),
            DurationMinutes = Math.Max(0, durationMinutes),
            OrderIndex = orderIndex,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
