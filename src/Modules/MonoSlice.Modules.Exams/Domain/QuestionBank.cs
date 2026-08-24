using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuestionBank : AggregateRoot<Guid>
{
    private readonly List<BankQuestion> _questions = [];

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<BankQuestion> Questions => _questions.AsReadOnly();

    private QuestionBank() : base(Guid.CreateVersion7()) { }

    public static QuestionBank Create(
        Guid createdBy,
        string title,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null)
    {
        if (createdBy == Guid.Empty)
        {
            throw new ValidationException("CreatedBy user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Question Bank title cannot be empty.");
        }

        return new QuestionBank
        {
            Id = Guid.CreateVersion7(),
            CreatedBy = createdBy,
            Title = title.Trim(),
            Description = description?.Trim(),
            Category = category?.Trim(),
            Tags = tags?.ToList() ?? [],
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        Guid updatedBy,
        string title,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null)
    {
        if (updatedBy == Guid.Empty)
        {
            throw new ValidationException("UpdatedBy user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Question Bank title cannot be empty.");
        }

        Title = title.Trim();
        Description = description?.Trim();
        Category = category?.Trim();
        Tags = tags?.ToList() ?? [];
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public BankQuestion AddQuestion(
        string questionText,
        QuestionType type,
        decimal points = 1m,
        string? explanation = null,
        IEnumerable<QuestionOption>? options = null)
    {
        var orderIndex = _questions.Count + 1;
        var question = BankQuestion.Create(
            Id,
            questionText,
            type,
            points,
            orderIndex,
            explanation,
            options);

        _questions.Add(question);
        UpdatedAtUtc = DateTime.UtcNow;
        return question;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question is not null)
        {
            _questions.Remove(question);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public BankQuestion UpdateQuestion(
        Guid questionId,
        string questionText,
        QuestionType type,
        decimal points,
        string? explanation = null,
        IEnumerable<QuestionOption>? options = null)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question is null)
        {
            throw new NotFoundException(nameof(BankQuestion), questionId);
        }

        question.Update(questionText, type, points, explanation, options);
        UpdatedAtUtc = DateTime.UtcNow;
        return question;
    }
}
