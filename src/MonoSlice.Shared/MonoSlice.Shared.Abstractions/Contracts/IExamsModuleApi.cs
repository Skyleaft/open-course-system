namespace MonoSlice.Shared.Abstractions.Contracts;

public record QuizExamContractDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    bool IsPublished);

public record QuizSubmissionContractDto(
    Guid Id,
    Guid QuizId,
    Guid StudentId,
    string Mode,
    DateTime StartedAtUtc,
    DateTime MaxAllowedEndTimeUtc,
    DateTime? FinishedAtUtc,
    string Status,
    decimal TotalScore,
    Guid? ActiveSessionToken);

public interface IExamsModuleApi
{
    Task<QuizExamContractDto?> GetExamByIdAsync(Guid quizId, CancellationToken ct = default);
    Task<QuizSubmissionContractDto?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken ct = default);
    Task<bool> ValidateActiveSessionAsync(Guid submissionId, Guid sessionToken, CancellationToken ct = default);
}
