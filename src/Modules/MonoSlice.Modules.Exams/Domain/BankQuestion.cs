using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class BankQuestion : Entity<Guid>
{
    public Guid BankId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; } = QuestionType.SingleChoice;
    public GradingMethod GradingMethod { get; private set; } = GradingMethod.PartialWithPenalty;
    public decimal Points { get; private set; } = 1m;
    public int OrderIndex { get; private set; }
    public string? Explanation { get; private set; }
    public List<QuestionOption> Options { get; private set; } = [];

    private BankQuestion() : base(Guid.CreateVersion7()) { }

    public static BankQuestion Create(
        Guid bankId,
        string questionText,
        QuestionType type,
        decimal points,
        int orderIndex,
        string? explanation = null,
        IEnumerable<QuestionOption>? options = null,
        GradingMethod? gradingMethod = null)
    {
        if (bankId == Guid.Empty)
        {
            throw new ValidationException("Bank ID is required.");
        }

        if (string.IsNullOrWhiteSpace(questionText))
        {
            throw new ValidationException("Question text cannot be empty.");
        }

        if (points <= 0)
        {
            throw new ValidationException("Question points must be greater than zero.");
        }

        var defaultMethod = type == QuestionType.MultipleChoice
            ? (gradingMethod ?? GradingMethod.PartialWithPenalty)
            : (gradingMethod ?? GradingMethod.AllOrNothing);

        return new BankQuestion
        {
            Id = Guid.CreateVersion7(),
            BankId = bankId,
            QuestionText = questionText.Trim(),
            Type = type,
            GradingMethod = defaultMethod,
            Points = points,
            OrderIndex = orderIndex,
            Explanation = explanation?.Trim(),
            Options = options?.ToList() ?? []
        };
    }

    public void Update(
        string questionText,
        QuestionType type,
        decimal points,
        string? explanation = null,
        IEnumerable<QuestionOption>? options = null,
        int? orderIndex = null,
        GradingMethod? gradingMethod = null)
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
        if (gradingMethod.HasValue)
        {
            GradingMethod = gradingMethod.Value;
        }
        else if (type == QuestionType.MultipleChoice && GradingMethod == GradingMethod.AllOrNothing)
        {
            GradingMethod = GradingMethod.PartialWithPenalty;
        }

        Points = points;
        Explanation = explanation?.Trim();
        Options = options?.ToList() ?? [];
        if (orderIndex.HasValue)
        {
            OrderIndex = orderIndex.Value;
        }
    }
}
