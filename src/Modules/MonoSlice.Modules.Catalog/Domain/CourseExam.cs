using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class CourseExam : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public Guid ExamId { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsMandatory { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private CourseExam() : base(Guid.CreateVersion7()) { }

    public static CourseExam Create(
        Guid courseId,
        Guid examId,
        int orderIndex,
        bool isMandatory = true)
    {
        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required.");
        }

        if (examId == Guid.Empty)
        {
            throw new ValidationException("Exam ID is required.");
        }

        return new CourseExam
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            ExamId = examId,
            OrderIndex = orderIndex,
            IsMandatory = isMandatory,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(int orderIndex, bool isMandatory)
    {
        OrderIndex = orderIndex;
        IsMandatory = isMandatory;
    }
}
