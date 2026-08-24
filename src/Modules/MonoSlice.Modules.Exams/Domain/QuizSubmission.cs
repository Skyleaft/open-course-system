using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class QuizSubmission : AggregateRoot<Guid>
{
    public Guid ExamId { get; private set; }
    public Guid StudentId { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public int DurationMinutes { get; private set; } = 60;
    public DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime MaxAllowedEndTimeUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public SubmissionStatus Status { get; private set; } = SubmissionStatus.InProgress;
    public int RandomSeed { get; private set; }
    public string ActiveSessionToken { get; private set; } = string.Empty;
    public decimal? Score { get; private set; }
    public bool? IsPassed { get; private set; }
    public List<ViolationRecord> Violations { get; private set; } = [];

    private readonly List<StudentAnswer> _answers = [];
    public IReadOnlyList<StudentAnswer> Answers => _answers.AsReadOnly();

    private readonly List<ProctoringSnapshot> _snapshots = [];
    public IReadOnlyList<ProctoringSnapshot> Snapshots => _snapshots.AsReadOnly();

    private QuizSubmission() : base(Guid.CreateVersion7()) { }

    public static QuizSubmission Create(
        Guid examId,
        Guid studentId,
        int durationMinutes,
        int randomSeed,
        string activeSessionToken,
        int attemptNumber = 1,
        DateTime? availableToUtc = null)
    {
        if (examId == Guid.Empty)
        {
            throw new ValidationException("Exam ID is required.");
        }

        if (studentId == Guid.Empty)
        {
            throw new ValidationException("Student ID is required.");
        }

        var startedAt = DateTime.UtcNow;
        var naturalEnd = startedAt.AddMinutes(durationMinutes);
        var maxAllowedEnd = availableToUtc.HasValue && availableToUtc.Value < naturalEnd
            ? availableToUtc.Value
            : naturalEnd;

        return new QuizSubmission
        {
            Id = Guid.CreateVersion7(),
            ExamId = examId,
            StudentId = studentId,
            AttemptNumber = Math.Max(1, attemptNumber),
            DurationMinutes = durationMinutes,
            StartedAtUtc = startedAt,
            MaxAllowedEndTimeUtc = maxAllowedEnd,
            Status = SubmissionStatus.InProgress,
            RandomSeed = randomSeed,
            ActiveSessionToken = activeSessionToken
        };
    }

    public StudentAnswer SaveAnswer(Guid questionId, IEnumerable<Guid>? selectedOptionIds, string? essayText)
    {
        if (Status != SubmissionStatus.InProgress)
        {
            throw new BusinessRuleException($"Cannot submit answer. Submission is already {Status}.");
        }

        if (DateTime.UtcNow > MaxAllowedEndTimeUtc.AddMinutes(1)) // 1-minute grace for network latency
        {
            Status = SubmissionStatus.TimedOut;
            throw new BusinessRuleException("Exam time limit exceeded.");
        }

        var existing = _answers.FirstOrDefault(a => a.QuestionId == questionId);
        if (existing is not null)
        {
            existing.UpdateAnswer(selectedOptionIds, essayText);
            return existing;
        }

        var answer = StudentAnswer.Create(Id, questionId, selectedOptionIds, essayText);
        _answers.Add(answer);
        return answer;
    }

    public void RecordViolation(string type, string reason, int maxAllowedViolations = 3)
    {
        var record = new ViolationRecord(type, reason, DateTime.UtcNow);
        Violations.Add(record);

        if (Violations.Count >= maxAllowedViolations)
        {
            Status = SubmissionStatus.Disqualified;
        }
    }

    public ProctoringSnapshot LogSnapshot(string storageKey)
    {
        var snapshot = ProctoringSnapshot.Create(Id, storageKey);
        _snapshots.Add(snapshot);
        return snapshot;
    }

    public void Complete(decimal calculatedScore, decimal passingScore)
    {
        if (Status == SubmissionStatus.Disqualified)
        {
            throw new BusinessRuleException("Cannot complete a disqualified exam attempt.");
        }

        Status = SubmissionStatus.Completed;
        SubmittedAtUtc = DateTime.UtcNow;
        Score = Math.Clamp(calculatedScore, 0m, 100m);
        IsPassed = Score >= passingScore;
    }

    public void Disqualify(string reason)
    {
        Status = SubmissionStatus.Disqualified;
        SubmittedAtUtc = DateTime.UtcNow;
        Violations.Add(new ViolationRecord("DISQUALIFIED", reason, DateTime.UtcNow));
    }

    public void MarkTimedOut()
    {
        Status = SubmissionStatus.TimedOut;
        SubmittedAtUtc = DateTime.UtcNow;
    }
}
