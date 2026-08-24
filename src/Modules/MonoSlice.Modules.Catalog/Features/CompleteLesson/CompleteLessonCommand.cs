using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.CompleteLesson;

public sealed record CompleteLessonCommand(Guid CourseId, Guid LessonId, bool? IsCompleted = null) : ICommand<ApiResponse<LessonProgressResultDto>>;

public sealed record LessonProgressResultDto(
    Guid CourseId,
    Guid LessonId,
    bool IsCompleted,
    DateTime? CompletedAtUtc,
    decimal UpdatedCourseProgressPercent);
