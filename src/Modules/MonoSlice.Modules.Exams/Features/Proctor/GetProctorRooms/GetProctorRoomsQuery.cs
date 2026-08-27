using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorRooms;

public sealed record GetProctorRoomsQuery : IQuery<ApiResponse<IReadOnlyList<ProctorCourseRoomDto>>>;

public sealed record ProctorCourseRoomDto(
    Guid CourseId,
    string CourseTitle,
    string? CourseDescription,
    string? ThumbnailUrl,
    Guid InstructorId,
    string? InstructorName,
    int EnrolledStudentsCount,
    int TotalActiveCandidates,
    int TotalFlaggedViolations,
    IReadOnlyList<ProctorRoomExamDto> Exams);

public sealed record ProctorRoomExamDto(
    Guid ExamId,
    string Title,
    string? Description,
    int DurationMinutes,
    int TotalQuestions,
    ExamRuleConfigDto? RuleConfig,
    int ActiveCandidatesCount,
    int FlaggedCount,
    bool IsPublished);

public sealed record ExamRuleConfigDto(
    string? Name,
    int MaxAllowedViolations,
    bool ForceFullscreen,
    bool RequireCamera,
    int SnapshotIntervalSeconds,
    bool RequireMicrophone,
    bool CanTabSwitch,
    bool RestrictClipboardAndMouse);
