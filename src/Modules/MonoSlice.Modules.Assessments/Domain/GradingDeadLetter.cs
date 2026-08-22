using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Assessments.Domain;

public sealed class GradingDeadLetter : Entity<Guid>
{
    public string StreamMessageId { get; private set; } = string.Empty;
    public Guid SubmissionId { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string? StackTrace { get; private set; }
    public DateTime FailedAtUtc { get; private set; } = DateTime.UtcNow;
    public bool IsResolved { get; private set; }
    public string? PayloadJson { get; private set; }
    public int RetryCount { get; private set; }

    private GradingDeadLetter() : base(Guid.CreateVersion7()) { }

    public static GradingDeadLetter Create(
        string streamMessageId,
        Guid submissionId,
        string errorMessage,
        string? stackTrace,
        string? payloadJson = null,
        int retryCount = 3)
    {
        if (string.IsNullOrWhiteSpace(streamMessageId))
        {
            throw new ValidationException("Stream message ID is required.");
        }

        if (submissionId == Guid.Empty)
        {
            throw new ValidationException("Submission ID is required.");
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ValidationException("Error message is required.");
        }

        return new GradingDeadLetter
        {
            Id = Guid.CreateVersion7(),
            StreamMessageId = streamMessageId,
            SubmissionId = submissionId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
            PayloadJson = payloadJson,
            RetryCount = retryCount,
            FailedAtUtc = DateTime.UtcNow,
            IsResolved = false
        };
    }

    public void MarkResolved()
    {
        IsResolved = true;
    }
}
