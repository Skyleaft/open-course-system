using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class CourseEnrollment : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public DateTime EnrolledAtUtc { get; private set; } = DateTime.UtcNow;

    private CourseEnrollment() : base(Guid.CreateVersion7()) { }

    public static CourseEnrollment Create(Guid userId, Guid courseId)
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException("User ID is required for course enrollment.");
        }

        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required for course enrollment.");
        }

        return new CourseEnrollment
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CourseId = courseId,
            EnrolledAtUtc = DateTime.UtcNow
        };
    }
}
