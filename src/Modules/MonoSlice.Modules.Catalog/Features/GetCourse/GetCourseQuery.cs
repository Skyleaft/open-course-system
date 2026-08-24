using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetCourse;

public sealed record GetCourseQuery(Guid Id) : IQuery<ApiResponse<CourseCurriculumDto>>;

public sealed record CourseCurriculumDto(
    Guid Id,
    Guid InstructorId,
    string Title,
    string? Description,
    string AccessType,
    decimal Price,
    bool IsPublished,
    string? ThumbnailUrl,
    DateTime CreatedAtUtc,
    IReadOnlyList<SectionDto> Sections,
    IReadOnlyList<AssignmentDto> Assignments,
    IReadOnlyList<CourseExamDto> Exams,
    bool IsEnrolled = false,
    int EnrolledStudentsCount = 0);

public sealed record CourseExamDto(
    Guid Id,
    Guid ExamId,
    int OrderIndex,
    bool IsMandatory);

public sealed record SectionDto(
    Guid Id,
    string Title,
    int OrderIndex,
    IReadOnlyList<LessonDto> Lessons);

public sealed record LessonDto(
    Guid Id,
    string Title,
    string Type,
    string? ContentUrl,
    string? TextContent,
    int DurationMinutes,
    int OrderIndex);

public sealed record AssignmentDto(
    Guid Id,
    string Title,
    string Instruction,
    DateTime DeadlineUtc,
    decimal MaxScore);
