using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.GetCourseEnrollments;

public sealed class GetCourseEnrollmentsQueryHandler
    : IQueryHandler<GetCourseEnrollmentsQuery, ApiResponse<PaginatedList<CourseStudentEnrollmentDto>>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityModuleApi _identityModuleApi;
    private readonly IExamsModuleApi _examsModuleApi;

    public GetCourseEnrollmentsQueryHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IIdentityModuleApi identityModuleApi,
        IExamsModuleApi examsModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _identityModuleApi = identityModuleApi;
        _examsModuleApi = examsModuleApi;
    }

    public async ValueTask<ApiResponse<PaginatedList<CourseStudentEnrollmentDto>>> Handle(
        GetCourseEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var course = await _dbContext.Courses
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Assignments)
            .Include(c => c.Exams)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        if (course is null)
        {
            return ApiResponse.Fail<PaginatedList<CourseStudentEnrollmentDto>>("Course not found.", 404);
        }

        var isAdmin = _currentUser.Roles.Contains("Admin");
        var isInstructor = _currentUser.Roles.Contains("Instructor");

        if (!isAdmin && (!isInstructor || course.InstructorId != _currentUser.UserId.Value))
        {
            return ApiResponse.Fail<PaginatedList<CourseStudentEnrollmentDto>>(
                "You are not authorized to view enrollments for this course.", 403);
        }

        var totalLessons = course.Sections.SelectMany(s => s.Lessons).Count();
        var totalAssignments = course.Assignments.Count;
        var totalExams = course.Exams.Count;
        var totalItems = totalLessons + totalAssignments + totalExams;

        var enrollmentsQuery = _dbContext.Enrollments
            .Where(e => e.CourseId == request.CourseId)
            .AsNoTracking();

        var totalEnrollmentsCount = await enrollmentsQuery.CountAsync(cancellationToken);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var pagedEnrollments = await enrollmentsQuery
            .OrderByDescending(e => e.EnrolledAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pagedEnrollments.Count == 0)
        {
            var emptyList = new PaginatedList<CourseStudentEnrollmentDto>(
                new List<CourseStudentEnrollmentDto>(), totalEnrollmentsCount, pageIndex, pageSize);
            return ApiResponse.Ok(emptyList);
        }

        var studentIds = pagedEnrollments.Select(e => e.UserId).Distinct().ToList();
        var users = await _identityModuleApi.GetUsersByIdsAsync(studentIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id);

        // Fetch lesson progress for these students in this course
        var progresses = await _dbContext.LessonProgresses
            .Where(lp => lp.CourseId == request.CourseId && studentIds.Contains(lp.UserId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var progressGroup = progresses
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Fetch assignment submissions
        var assignmentIds = course.Assignments.Select(a => a.Id).ToList();
        var submissions = await _dbContext.Submissions
            .Where(s => assignmentIds.Contains(s.AssignmentId) && studentIds.Contains(s.StudentId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var submissionGroup = submissions
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.AssignmentId).Distinct().Count());

        // Fetch completed exams for these students
        var examIds = course.Exams.Select(e => e.ExamId).ToList();
        var examSubmissions = await _examsModuleApi.GetStudentsSubmissionsForExamsAsync(studentIds, examIds, cancellationToken);
        var completedExamsGroup = examSubmissions
            .Where(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.QuizId).Distinct().Count());

        // Fetch exam titles for course exams
        var examTitleDict = new Dictionary<Guid, string>();
        foreach (var cExam in course.Exams)
        {
            var examContract = await _examsModuleApi.GetExamByIdAsync(cExam.ExamId, cancellationToken);
            examTitleDict[cExam.ExamId] = examContract?.Title ?? "Examination";
        }

        // Group all exam submissions by student
        var studentExamSubmissionsGroup = examSubmissions
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<CourseStudentEnrollmentDto>();

        foreach (var enrollment in pagedEnrollments)
        {
            userDict.TryGetValue(enrollment.UserId, out var user);

            var studentProgressList = progressGroup.GetValueOrDefault(enrollment.UserId) ?? new List<Domain.LessonProgress>();
            var completedLessonsCount = studentProgressList.Count(p => p.IsCompleted);
            var completedAssignmentsCount = submissionGroup.GetValueOrDefault(enrollment.UserId);
            var completedExamsCount = completedExamsGroup.GetValueOrDefault(enrollment.UserId);

            var lastAccessedAt = studentProgressList
                .OrderByDescending(p => p.LastAccessedAtUtc)
                .Select(p => (DateTime?)p.LastAccessedAtUtc)
                .FirstOrDefault();

            var completedItems = completedLessonsCount + completedAssignmentsCount + completedExamsCount;
            var progressPercent = totalItems > 0
                ? Math.Round(((decimal)completedItems / totalItems) * 100m, 1)
                : (studentProgressList.Any() ? 100m : 0m);

            if (progressPercent > 100m) progressPercent = 100m;

            var fullName = user?.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user?.UserName ?? "Student";
            }

            var email = user?.Email ?? string.Empty;

            var studentSubmissions = studentExamSubmissionsGroup.GetValueOrDefault(enrollment.UserId) ?? new List<QuizSubmissionContractDto>();
            var studentExamsList = new List<CourseStudentExamProgressDto>();

            foreach (var cExam in course.Exams)
            {
                var sub = studentSubmissions
                    .Where(s => s.QuizId == cExam.ExamId)
                    .OrderByDescending(s => s.StartedAtUtc)
                    .FirstOrDefault();

                var examTitle = examTitleDict.GetValueOrDefault(cExam.ExamId) ?? "Examination";

                studentExamsList.Add(new CourseStudentExamProgressDto(
                    cExam.ExamId,
                    examTitle,
                    sub?.Status ?? "NotStarted",
                    sub?.TotalScore,
                    sub != null && string.Equals(sub.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? (bool?)(sub.TotalScore >= 70m) : null,
                    sub?.StartedAtUtc,
                    sub?.FinishedAtUtc,
                    sub?.Id));
            }

            items.Add(new CourseStudentEnrollmentDto(
                enrollment.Id,
                enrollment.UserId,
                fullName,
                email,
                user?.Picture,
                enrollment.EnrolledAtUtc,
                progressPercent,
                completedLessonsCount,
                totalLessons,
                completedAssignmentsCount,
                totalAssignments,
                lastAccessedAt,
                studentExamsList));
        }

        // Search filtering in-memory if search parameter is provided
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            items = items.Where(i =>
                i.FullName.ToLower().Contains(search) ||
                i.Email.ToLower().Contains(search)).ToList();
        }

        var result = new PaginatedList<CourseStudentEnrollmentDto>(
            items, totalEnrollmentsCount, pageIndex, pageSize);

        return ApiResponse.Ok(result);
    }
}
