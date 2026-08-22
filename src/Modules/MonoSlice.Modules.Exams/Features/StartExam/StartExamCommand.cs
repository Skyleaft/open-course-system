using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.StartExam;

public sealed record StartExamCommand(Guid ExamId) : ICommand<ApiResponse<ExamAttemptDto>>;

public sealed record ExamAttemptDto(
    Guid SubmissionId,
    Guid ExamId,
    string Title,
    string Mode,
    int AttemptNumber,
    int MaxAttempts,
    DateTime StartedAtUtc,
    DateTime MaxAllowedEndTimeUtc,
    DateTime? AvailableToUtc,
    string ActiveSessionToken,
    int TotalQuestions,
    int DurationMinutes);
