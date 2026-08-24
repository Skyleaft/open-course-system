using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetDeadLetters;

public sealed record GetDeadLettersQuery(bool? OnlyUnresolved = true) : IQuery<ApiResponse<IReadOnlyList<DeadLetterDto>>>;

public sealed record DeadLetterDto(
    Guid Id,
    string StreamMessageId,
    Guid SubmissionId,
    string ErrorMessage,
    string? StackTrace,
    DateTime FailedAtUtc,
    bool IsResolved,
    int RetryCount,
    string? PayloadJson);
