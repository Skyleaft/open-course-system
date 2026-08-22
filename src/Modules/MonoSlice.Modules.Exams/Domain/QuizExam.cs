using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuizExam : AggregateRoot<Guid>
{
    public Guid? CourseId { get; private set; }
    public Guid InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public QuizMode Mode { get; private set; } = QuizMode.RealExam;
    public int DurationMinutes { get; private set; } = 60;
    public decimal PassingScore { get; private set; } = 70m;
    public int MaxAllowedViolations { get; private set; } = 3;
    public int MaxAttempts { get; private set; } = 1;
    public DateTime? AvailableFromUtc { get; private set; }
    public DateTime? AvailableToUtc { get; private set; }
    public bool IsPublished { get; private set; }
    public bool ShuffleQuestions { get; private set; } = true;
    public bool ShuffleOptions { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<QuizQuestion> _questions = [];
    public IReadOnlyList<QuizQuestion> Questions => _questions.AsReadOnly();

    private QuizExam() : base(Guid.CreateVersion7()) { }

    public static QuizExam Create(
        Guid instructorId,
        string title,
        string? description,
        QuizMode mode,
        int durationMinutes,
        decimal passingScore,
        int maxAllowedViolations = 3,
        int maxAttempts = 1,
        DateTime? availableFromUtc = null,
        DateTime? availableToUtc = null,
        Guid? courseId = null,
        bool shuffleQuestions = true,
        bool shuffleOptions = true)
    {
        if (instructorId == Guid.Empty)
        {
            throw new ValidationException("Instructor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Exam title cannot be empty.");
        }

        if (durationMinutes <= 0)
        {
            throw new BusinessRuleException("Exam duration must be greater than zero minutes.");
        }

        if (passingScore < 0 || passingScore > 100)
        {
            throw new BusinessRuleException("Passing score must be between 0 and 100.");
        }

        if (maxAttempts <= 0)
        {
            throw new BusinessRuleException("Max attempts must be at least 1.");
        }

        if (availableFromUtc.HasValue && availableToUtc.HasValue && availableToUtc.Value <= availableFromUtc.Value)
        {
            throw new BusinessRuleException("Exam closing time (AvailableToUtc) must be after opening time (AvailableFromUtc).");
        }

        return new QuizExam
        {
            Id = Guid.CreateVersion7(),
            InstructorId = instructorId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Mode = mode,
            DurationMinutes = durationMinutes,
            PassingScore = passingScore,
            MaxAllowedViolations = Math.Max(1, maxAllowedViolations),
            MaxAttempts = maxAttempts,
            AvailableFromUtc = availableFromUtc,
            AvailableToUtc = availableToUtc,
            CourseId = courseId,
            ShuffleQuestions = shuffleQuestions,
            ShuffleOptions = shuffleOptions,
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string? description,
        QuizMode mode,
        int durationMinutes,
        decimal passingScore,
        int maxAllowedViolations,
        int maxAttempts,
        DateTime? availableFromUtc,
        DateTime? availableToUtc,
        Guid? courseId,
        bool shuffleQuestions,
        bool shuffleOptions)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Exam title cannot be empty.");
        }

        if (durationMinutes <= 0)
        {
            throw new BusinessRuleException("Exam duration must be greater than zero minutes.");
        }

        if (passingScore < 0 || passingScore > 100)
        {
            throw new BusinessRuleException("Passing score must be between 0 and 100.");
        }

        if (maxAttempts <= 0)
        {
            throw new BusinessRuleException("Max attempts must be at least 1.");
        }

        if (availableFromUtc.HasValue && availableToUtc.HasValue && availableToUtc.Value <= availableFromUtc.Value)
        {
            throw new BusinessRuleException("Exam closing time (AvailableToUtc) must be after opening time (AvailableFromUtc).");
        }

        Title = title.Trim();
        Description = description?.Trim();
        Mode = mode;
        DurationMinutes = durationMinutes;
        PassingScore = passingScore;
        MaxAllowedViolations = Math.Max(1, maxAllowedViolations);
        MaxAttempts = maxAttempts;
        AvailableFromUtc = availableFromUtc;
        AvailableToUtc = availableToUtc;
        CourseId = courseId;
        ShuffleQuestions = shuffleQuestions;
        ShuffleOptions = shuffleOptions;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (_questions.Count == 0)
        {
            throw new BusinessRuleException("Cannot publish an exam without questions.");
        }

        IsPublished = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public QuizQuestion AddQuestion(
        string questionText,
        QuestionType type,
        decimal points,
        string? explanation,
        IEnumerable<QuestionOption>? options = null)
    {
        var question = QuizQuestion.Create(
            Id,
            questionText,
            type,
            points,
            _questions.Count + 1,
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
}
