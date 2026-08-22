using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuizQuestion : Entity<Guid>
{
    public Guid ExamId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; } = QuestionType.SingleChoice;
    public decimal Points { get; private set; } = 1m;
    public int OrderIndex { get; private set; }
    public string? Explanation { get; private set; }
    public List<QuestionOption> Options { get; private set; } = [];

    private QuizQuestion() : base(Guid.CreateVersion7()) { }

    public static QuizQuestion Create(
        Guid examId,
        string questionText,
        QuestionType type,
        decimal points,
        int orderIndex,
        string? explanation,
        IEnumerable<QuestionOption>? options = null)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            throw new ValidationException("Question text cannot be empty.");
        }

        if (points <= 0)
        {
            throw new ValidationException("Question points must be greater than zero.");
        }

        var question = new QuizQuestion
        {
            Id = Guid.CreateVersion7(),
            ExamId = examId,
            QuestionText = questionText.Trim(),
            Type = type,
            Points = points,
            OrderIndex = orderIndex,
            Explanation = explanation?.Trim(),
            Options = options?.ToList() ?? []
        };

        return question;
    }

    public void Update(
        string questionText,
        QuestionType type,
        decimal points,
        string? explanation,
        IEnumerable<QuestionOption>? options)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            throw new ValidationException("Question text cannot be empty.");
        }

        if (points <= 0)
        {
            throw new ValidationException("Question points must be greater than zero.");
        }

        QuestionText = questionText.Trim();
        Type = type;
        Points = points;
        Explanation = explanation?.Trim();
        Options = options?.ToList() ?? [];
    }
}
