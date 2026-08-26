using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuizExam : AggregateRoot<Guid>
{
    public Guid InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ExamRuleId { get; private set; }
    public ExamRuleConfig RuleConfig { get; private set; } = ExamRuleConfig.StrictProctored();
    public int DurationMinutes { get; private set; } = 60;
    public decimal PassingScore { get; private set; } = 70m;
    public int MaxAttempts { get; private set; } = 1;
    public DateTime? AvailableFromUtc { get; private set; }
    public DateTime? AvailableToUtc { get; private set; }
    public bool IsPublished { get; private set; }
    public bool ShuffleQuestions { get; private set; } = true;
    public bool ShuffleOptions { get; private set; } = true;
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<QuizSection> _sections = [];
    public IReadOnlyList<QuizSection> Sections => _sections.AsReadOnly();

    private QuizExam() : base(Guid.CreateVersion7()) { }

    public static QuizExam Create(
        Guid instructorId,
        string title,
        string? description,
        int durationMinutes,
        decimal passingScore,
        Guid? examRuleId = null,
        ExamRuleConfig? ruleConfig = null,
        int maxAttempts = 1,
        DateTime? availableFromUtc = null,
        DateTime? availableToUtc = null,
        bool shuffleQuestions = true,
        bool shuffleOptions = true,
        Guid? createdBy = null)
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

        var creator = createdBy ?? instructorId;

        return new QuizExam
        {
            Id = Guid.CreateVersion7(),
            InstructorId = instructorId,
            Title = title.Trim(),
            Description = description?.Trim(),
            ExamRuleId = examRuleId,
            RuleConfig = ruleConfig ?? ExamRuleConfig.StrictProctored(),
            DurationMinutes = durationMinutes,
            PassingScore = passingScore,
            MaxAttempts = maxAttempts,
            AvailableFromUtc = availableFromUtc,
            AvailableToUtc = availableToUtc,
            ShuffleQuestions = shuffleQuestions,
            ShuffleOptions = shuffleOptions,
            IsPublished = false,
            CreatedBy = creator,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string? description,
        int durationMinutes,
        decimal passingScore,
        Guid? examRuleId,
        ExamRuleConfig? ruleConfig,
        int maxAttempts,
        DateTime? availableFromUtc,
        DateTime? availableToUtc,
        bool shuffleQuestions,
        bool shuffleOptions,
        Guid? updatedBy = null)
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
        ExamRuleId = examRuleId;
        if (ruleConfig != null)
        {
            RuleConfig = ruleConfig;
        }
        DurationMinutes = durationMinutes;
        PassingScore = passingScore;
        MaxAttempts = maxAttempts;
        AvailableFromUtc = availableFromUtc;
        AvailableToUtc = availableToUtc;
        ShuffleQuestions = shuffleQuestions;
        ShuffleOptions = shuffleOptions;
        UpdatedBy = updatedBy ?? InstructorId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (_sections.Count == 0)
        {
            throw new BusinessRuleException("Cannot publish an exam without sections.");
        }

        IsPublished = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public QuizSection AddSection(
        Guid questionBankId,
        string title,
        decimal? pointsOverride = null,
        int? questionCount = null,
        string? description = null)
    {
        var section = QuizSection.Create(
            Id,
            questionBankId,
            title,
            _sections.Count + 1,
            pointsOverride,
            questionCount,
            description);

        _sections.Add(section);
        UpdatedAtUtc = DateTime.UtcNow;
        return section;
    }

    public void RemoveSection(Guid sectionId)
    {
        var section = _sections.FirstOrDefault(s => s.Id == sectionId);
        if (section is not null)
        {
            _sections.Remove(section);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
