using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class Assignment : Entity<Guid>
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Instruction { get; private set; } = string.Empty;
    public DateTime DeadlineUtc { get; private set; }
    public decimal MaxScore { get; private set; } = 100m;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Assignment() : base(Guid.CreateVersion7()) { }

    public static Assignment Create(
        Guid courseId,
        string title,
        string instruction,
        DateTime deadlineUtc,
        decimal maxScore = 100m)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Assignment title cannot be empty.");
        }

        if (maxScore <= 0)
        {
            throw new BusinessRuleException("Assignment max score must be greater than zero.");
        }

        return new Assignment
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            Title = title.Trim(),
            Instruction = instruction.Trim(),
            DeadlineUtc = deadlineUtc,
            MaxScore = maxScore,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
