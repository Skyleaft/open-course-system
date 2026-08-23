namespace MonoSlice.Shared.Abstractions.Messaging;

public sealed record ExamDeletedIntegrationEvent(
    Guid ExamId,
    Guid InstructorId,
    Guid? CourseId) : IntegrationEvent;
