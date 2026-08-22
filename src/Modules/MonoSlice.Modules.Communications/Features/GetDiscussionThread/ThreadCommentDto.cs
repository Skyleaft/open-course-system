namespace MonoSlice.Modules.Communications.Features.GetDiscussionThread;

public sealed record ThreadCommentDto(
    Guid Id,
    Guid ThreadId,
    Guid AuthorId,
    Guid? ParentCommentId,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<ThreadCommentDto> Replies);
