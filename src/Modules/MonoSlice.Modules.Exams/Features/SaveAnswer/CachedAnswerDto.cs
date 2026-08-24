namespace MonoSlice.Modules.Exams.Features.SaveAnswer;

public sealed record CachedAnswerDto(
    Guid QuestionId,
    List<Guid>? SelectedOptionIds,
    string? EssayText,
    DateTime SavedAtUtc);
