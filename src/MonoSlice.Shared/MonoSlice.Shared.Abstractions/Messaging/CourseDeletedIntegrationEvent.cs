using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Shared.Abstractions.Messaging;

/// <summary>
/// Integration event published when a course is deleted, allowing other modules
/// (e.g. Communications, Exams, Assessments) to asynchronously cascade cleanup of related records.
/// </summary>
public sealed record CourseDeletedIntegrationEvent(
    Guid CourseId,
    Guid InstructorId) : IntegrationEvent;
