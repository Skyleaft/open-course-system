namespace MonoSlice.Modules.Communications.Features.CreateDiscussionThread;

public sealed record DiscussionThreadSummaryDto(
    Guid Id,
    Guid CourseId,
    Guid? LessonId,
    Guid AuthorId,
    string Title,
    string Content,
    bool IsClosed,
    int CommentsCount,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    Guid? ClosedByUserId);
