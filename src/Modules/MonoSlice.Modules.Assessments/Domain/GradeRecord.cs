using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Assessments.Domain;

public sealed class GradeRecord : Entity<Guid>
{
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public GradeItemType ItemType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public decimal Score { get; private set; }
    public decimal MaxScore { get; private set; }
    public decimal WeightPercentage { get; private set; } = 100.00m;
    public DateTime EvaluatedAtUtc { get; private set; } = DateTime.UtcNow;

    private GradeRecord() : base(Guid.CreateVersion7()) { }

    public static GradeRecord Create(
        Guid studentId,
        Guid courseId,
        GradeItemType itemType,
        Guid referenceId,
        decimal score,
        decimal maxScore,
        decimal weightPercentage = 100.00m)
    {
        if (studentId == Guid.Empty)
        {
            throw new ValidationException("Student ID is required.");
        }

        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required.");
        }

        if (referenceId == Guid.Empty)
        {
            throw new ValidationException("Reference ID is required.");
        }

        if (score < 0)
        {
            throw new BusinessRuleException("Score cannot be negative.");
        }

        if (maxScore <= 0)
        {
            throw new BusinessRuleException("Max score must be greater than zero.");
        }

        return new GradeRecord
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            CourseId = courseId,
            ItemType = itemType,
            ReferenceId = referenceId,
            Score = score,
            MaxScore = maxScore,
            WeightPercentage = Math.Max(0m, weightPercentage),
            EvaluatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateScore(decimal newScore)
    {
        if (newScore < 0)
        {
            throw new BusinessRuleException("Score cannot be negative.");
        }

        Score = newScore;
        EvaluatedAtUtc = DateTime.UtcNow;
    }
}
