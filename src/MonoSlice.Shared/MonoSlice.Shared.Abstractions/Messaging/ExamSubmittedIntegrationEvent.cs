namespace MonoSlice.Shared.Abstractions.Messaging;

public sealed record ExamSubmittedIntegrationEvent(
    Guid SubmissionId,
    Guid ExamId,
    Guid StudentId,
    decimal Score,
    bool IsPassed,
    DateTime SubmittedAtUtc) : IntegrationEvent;
