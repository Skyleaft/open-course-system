namespace MonoSlice.Modules.Communications.Features.CreateAnnouncement;

public sealed record AnnouncementDto(
    Guid Id,
    Guid? CourseId,
    Guid AuthorId,
    string Title,
    string Content,
    bool IsPinned,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
