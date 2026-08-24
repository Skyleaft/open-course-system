using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuizSection : Entity<Guid>
{
    public Guid ExamId { get; private set; }
    public Guid QuestionBankId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int OrderIndex { get; private set; }
    public decimal? PointsOverride { get; private set; }
    public int? QuestionCount { get; private set; }

    public QuestionBank? QuestionBank { get; private set; }

    private QuizSection() : base(Guid.CreateVersion7()) { }

    public static QuizSection Create(
        Guid examId,
        Guid questionBankId,
        string title,
        int orderIndex,
        decimal? pointsOverride = null,
        int? questionCount = null,
        string? description = null)
    {
        if (examId == Guid.Empty)
        {
            throw new ValidationException("Exam ID is required.");
        }

        if (questionBankId == Guid.Empty)
        {
            throw new ValidationException("Question Bank ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Section title cannot be empty.");
        }

        if (pointsOverride.HasValue && pointsOverride.Value <= 0)
        {
            throw new ValidationException("Points override must be greater than zero.");
        }

        if (questionCount.HasValue && questionCount.Value <= 0)
        {
            throw new ValidationException("Question count must be greater than zero.");
        }

        return new QuizSection
        {
            Id = Guid.CreateVersion7(),
            ExamId = examId,
            QuestionBankId = questionBankId,
            Title = title.Trim(),
            Description = description?.Trim(),
            OrderIndex = orderIndex,
            PointsOverride = pointsOverride,
            QuestionCount = questionCount
        };
    }

    public void Update(
        Guid questionBankId,
        string title,
        int orderIndex,
        decimal? pointsOverride = null,
        int? questionCount = null,
        string? description = null)
    {
        if (questionBankId == Guid.Empty)
        {
            throw new ValidationException("Question Bank ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Section title cannot be empty.");
        }

        if (pointsOverride.HasValue && pointsOverride.Value <= 0)
        {
            throw new ValidationException("Points override must be greater than zero.");
        }

        if (questionCount.HasValue && questionCount.Value <= 0)
        {
            throw new ValidationException("Question count must be greater than zero.");
        }

        QuestionBankId = questionBankId;
        Title = title.Trim();
        Description = description?.Trim();
        OrderIndex = orderIndex;
        PointsOverride = pointsOverride;
        QuestionCount = questionCount;
    }
}
