using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class CourseSection : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int OrderIndex { get; private set; }

    private readonly List<Lesson> _lessons = [];
    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();

    private CourseSection() : base(Guid.CreateVersion7()) { }

    public static CourseSection Create(Guid courseId, string title, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Section title cannot be empty.");
        }

        return new CourseSection
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            Title = title.Trim(),
            OrderIndex = orderIndex
        };
    }

    public Lesson AddLesson(string title, LessonType type, string contentUrl, int durationMinutes)
    {
        var lesson = Lesson.Create(Id, title, type, contentUrl, durationMinutes, _lessons.Count + 1);
        _lessons.Add(lesson);
        return lesson;
    }
}
