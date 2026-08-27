using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorRooms;

public sealed class GetProctorRoomsQueryHandler : IQueryHandler<GetProctorRoomsQuery, ApiResponse<IReadOnlyList<ProctorCourseRoomDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICoursesModuleApi _coursesModuleApi;
    private readonly IIdentityModuleApi _identityModuleApi;
    private readonly ICurrentUser _currentUser;

    public GetProctorRoomsQueryHandler(
        ExamsDbContext dbContext,
        ICoursesModuleApi coursesModuleApi,
        IIdentityModuleApi identityModuleApi,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _coursesModuleApi = coursesModuleApi;
        _identityModuleApi = identityModuleApi;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<ProctorCourseRoomDto>>> Handle(
        GetProctorRoomsQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Fetch courses that have attached exams from Catalog module
        var coursesWithExams = await _coursesModuleApi.GetCoursesWithExamsAsync(cancellationToken);
        if (coursesWithExams.Count == 0)
        {
            return ApiResponse.Ok<IReadOnlyList<ProctorCourseRoomDto>>(Array.Empty<ProctorCourseRoomDto>());
        }

        // 2. Fetch instructor identity details in bulk
        var instructorIds = coursesWithExams.Select(c => c.InstructorId).Distinct().ToList();
        var instructorProfiles = await _identityModuleApi.GetUsersByIdsAsync(instructorIds, cancellationToken);
        var instructorNameMap = instructorProfiles.ToDictionary(
            u => u.Id,
            u => u.FullName ?? u.UserName);

        // 3. Collect all exam IDs
        var allExamIds = coursesWithExams.SelectMany(c => c.Exams.Select(e => e.ExamId)).Distinct().ToList();

        // 4. Fetch Exam entities with sections
        var examEntities = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
            .Where(e => allExamIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        // 5. Fetch live active candidate metrics (submissions with Status == InProgress)
        var liveSubmissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => allExamIds.Contains(s.ExamId) && s.Status == SubmissionStatus.InProgress)
            .Select(s => new { s.ExamId, ViolationCount = s.Violations.Count })
            .ToListAsync(cancellationToken);

        var examActiveCountMap = liveSubmissions
            .GroupBy(s => s.ExamId)
            .ToDictionary(g => g.Key, g => g.Count());

        var examFlaggedCountMap = liveSubmissions
            .Where(s => s.ViolationCount > 0)
            .GroupBy(s => s.ExamId)
            .ToDictionary(g => g.Key, g => g.Count());

        // 6. Build the course room DTOs
        var result = new List<ProctorCourseRoomDto>();
        foreach (var course in coursesWithExams)
        {
            var roomExams = new List<ProctorRoomExamDto>();
            int courseActiveCandidates = 0;
            int courseFlaggedViolations = 0;

            foreach (var courseExam in course.Exams)
            {
                if (!examEntities.TryGetValue(courseExam.ExamId, out var exam))
                {
                    continue;
                }

                var activeCount = examActiveCountMap.GetValueOrDefault(exam.Id, 0);
                var flaggedCount = examFlaggedCountMap.GetValueOrDefault(exam.Id, 0);
                courseActiveCandidates += activeCount;
                courseFlaggedViolations += flaggedCount;

                var totalQuestions = exam.Sections.Sum(s => s.QuestionCount ?? 0);

                ExamRuleConfigDto? ruleConfigDto = null;
                if (exam.RuleConfig is not null)
                {
                    ruleConfigDto = new ExamRuleConfigDto(
                        exam.RuleConfig.Name,
                        exam.RuleConfig.MaxAllowedViolations,
                        exam.RuleConfig.ForceFullscreen,
                        exam.RuleConfig.RequireCamera,
                        exam.RuleConfig.SnapshotIntervalSeconds,
                        exam.RuleConfig.RequireMicrophone,
                        exam.RuleConfig.CanTabSwitch,
                        exam.RuleConfig.RestrictClipboardAndMouse);
                }

                roomExams.Add(new ProctorRoomExamDto(
                    exam.Id,
                    exam.Title,
                    exam.Description,
                    exam.DurationMinutes,
                    totalQuestions,
                    ruleConfigDto,
                    activeCount,
                    flaggedCount,
                    exam.IsPublished));
            }

            if (roomExams.Count > 0)
            {
                result.Add(new ProctorCourseRoomDto(
                    course.CourseId,
                    course.CourseTitle,
                    course.CourseDescription,
                    course.ThumbnailUrl,
                    course.InstructorId,
                    instructorNameMap.GetValueOrDefault(course.InstructorId),
                    course.EnrolledStudentsCount,
                    courseActiveCandidates,
                    courseFlaggedViolations,
                    roomExams));
            }
        }

        return ApiResponse.Ok<IReadOnlyList<ProctorCourseRoomDto>>(result);
    }
}
