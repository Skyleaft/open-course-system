namespace MonoSlice.Modules.Communications.Features.GetDiscussionThread;

public sealed record DiscussionThreadDetailDto(
    Guid Id,
    Guid CourseId,
    Guid? LessonId,
    Guid AuthorId,
    string Title,
    string Content,
    bool IsClosed,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    Guid? ClosedByUserId,
    IReadOnlyList<ThreadCommentDto> Comments);
