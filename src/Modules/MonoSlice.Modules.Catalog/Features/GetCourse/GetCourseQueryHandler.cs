using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.GetCourse;

public sealed class GetCourseQueryHandler : IQueryHandler<GetCourseQuery, ApiResponse<CourseCurriculumDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IExamsModuleApi _examsModuleApi;

    public GetCourseQueryHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser,
        IExamsModuleApi examsModuleApi)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _examsModuleApi = examsModuleApi;
    }

    public async ValueTask<ApiResponse<CourseCurriculumDto>> Handle(
        GetCourseQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"course:{query.Id}";
        var cachedCourse = await _cacheService.GetAsync<CourseCurriculumDto>(cacheKey, cancellationToken);

        CourseCurriculumDto dto;
        if (cachedCourse is not null)
        {
            dto = cachedCourse;
        }
        else
        {
            var course = await _dbContext.Courses
                .AsNoTracking()
                .Include(c => c.Sections.OrderBy(s => s.OrderIndex))
                    .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
                .Include(c => c.Assignments.OrderBy(a => a.DeadlineUtc))
                .Include(c => c.Exams.OrderBy(e => e.OrderIndex))
                .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException(nameof(Course), query.Id);
            }

            var sections = course.Sections.Select(s => new SectionDto(
                s.Id,
                s.Title,
                s.OrderIndex,
                s.Lessons.Select(l => new LessonDto(
                    l.Id,
                    l.Title,
                    l.Type.ToString(),
                    l.ContentUrl,
                    l.TextContent,
                    l.DurationMinutes,
                    l.OrderIndex)).ToList()
            )).ToList();

            var assignments = course.Assignments.Select(a => new AssignmentDto(
                a.Id,
                a.Title,
                a.Instruction,
                a.DeadlineUtc,
                a.MaxScore
            )).ToList();

            var exams = new List<CourseExamDto>();
            foreach (var e in course.Exams)
            {
                var examContract = await _examsModuleApi.GetExamByIdAsync(e.ExamId, cancellationToken);
                exams.Add(new CourseExamDto(
                    e.Id,
                    e.ExamId,
                    e.OrderIndex,
                    e.IsMandatory,
                    examContract?.Title ?? "Course Final Examination"
                ));
            }

            dto = new CourseCurriculumDto(
                course.Id,
                course.InstructorId,
                course.Title,
                course.Description,
                course.AccessType.ToString(),
                course.Price,
                course.IsPublished,
                course.ThumbnailUrl,
                course.CreatedAtUtc,
                sections,
                assignments,
                exams);

            await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);
        }

        // Check user enrollment & count
        var isEnrolled = false;
        if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
        {
            isEnrolled = await _dbContext.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.UserId == _currentUser.UserId.Value && e.CourseId == query.Id, cancellationToken);
        }

        var enrolledStudentsCount = await _dbContext.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.CourseId == query.Id, cancellationToken);

        var resultDto = dto with
        {
            IsEnrolled = isEnrolled,
            EnrolledStudentsCount = enrolledStudentsCount
        };
        return ApiResponse.Ok(resultDto);
    }
}
