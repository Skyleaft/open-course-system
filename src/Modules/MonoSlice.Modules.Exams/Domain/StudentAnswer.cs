using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class StudentAnswer : Entity<Guid>
{
    public Guid SubmissionId { get; private set; }
    public Guid QuestionId { get; private set; }
    public List<Guid> SelectedOptionIds { get; private set; } = [];
    public string? EssayText { get; private set; }
    public decimal? AwardedScore { get; private set; }
    public DateTime AnsweredAtUtc { get; private set; } = DateTime.UtcNow;

    private StudentAnswer() : base(Guid.CreateVersion7()) { }

    public static StudentAnswer Create(
        Guid submissionId,
        Guid questionId,
        IEnumerable<Guid>? selectedOptionIds,
        string? essayText)
    {
        return new StudentAnswer
        {
            Id = Guid.CreateVersion7(),
            SubmissionId = submissionId,
            QuestionId = questionId,
            SelectedOptionIds = selectedOptionIds?.ToList() ?? [],
            EssayText = essayText?.Trim(),
            AnsweredAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateAnswer(IEnumerable<Guid>? selectedOptionIds, string? essayText)
    {
        SelectedOptionIds = selectedOptionIds?.ToList() ?? [];
        EssayText = essayText?.Trim();
        AnsweredAtUtc = DateTime.UtcNow;
    }

    public void SetAwardedScore(decimal score)
    {
        AwardedScore = Math.Max(0, score);
    }
}
