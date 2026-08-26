using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetCourseProgress;

public sealed record GetCourseProgressQuery(Guid CourseId) : IQuery<ApiResponse<CourseProgressDto>>;

public sealed record CourseProgressDto(
    Guid CourseId,
    IReadOnlyList<Guid> CompletedLessonIds,
    IReadOnlyList<Guid> CompletedAssignmentIds,
    IReadOnlyList<Guid> CompletedExamIds,
    decimal ProgressPercent,
    Guid? LastAccessedLessonId);
