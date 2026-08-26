namespace MonoSlice.Shared.Abstractions.Messaging;

public sealed record StudentUnenrolledIntegrationEvent(
    Guid CourseId,
    Guid UserId,
    Guid EnrollmentId,
    IReadOnlyList<Guid> ExamIds,
    DateTime UnenrolledAtUtc) : IntegrationEvent;
