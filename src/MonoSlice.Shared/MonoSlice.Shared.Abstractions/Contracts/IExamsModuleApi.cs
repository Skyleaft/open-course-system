namespace MonoSlice.Shared.Abstractions.Contracts;

public record QuizExamContractDto(
    Guid Id,
    string Title,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    bool IsPublished,
    Guid? CourseId = null);

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
    Task<IReadOnlyList<QuizSubmissionContractDto>> GetStudentSubmissionsForExamsAsync(Guid studentId, IEnumerable<Guid> examIds, CancellationToken ct = default);
    Task<IReadOnlyList<QuizSubmissionContractDto>> GetStudentsSubmissionsForExamsAsync(IEnumerable<Guid> studentIds, IEnumerable<Guid> examIds, CancellationToken ct = default);
}
