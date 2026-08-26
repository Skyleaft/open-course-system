using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetExamSubmissions;

public sealed class GetExamSubmissionsQueryHandler
    : IQueryHandler<GetExamSubmissionsQuery, ApiResponse<PaginatedList<ExamSubmissionDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityModuleApi _identityModuleApi;
    private readonly ICoursesModuleApi _coursesModuleApi;

    public GetExamSubmissionsQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        IIdentityModuleApi identityModuleApi,
        ICoursesModuleApi coursesModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _identityModuleApi = identityModuleApi;
        _coursesModuleApi = coursesModuleApi;
    }

    public async ValueTask<ApiResponse<PaginatedList<ExamSubmissionDto>>> Handle(
        GetExamSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var exam = await _dbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), request.ExamId);
        }

        var isAdmin = _currentUser.Roles.Contains("Admin");
        var isInstructor = _currentUser.Roles.Contains("Instructor");

        if (!isAdmin)
        {
            var isCreator = exam.CreatedBy == _currentUser.UserId.Value;
            var courseId = await _coursesModuleApi.GetCourseIdForExamAsync(exam.Id, cancellationToken);
            var isCourseInstructor = false;

            if (courseId.HasValue)
            {
                isCourseInstructor = isInstructor;
            }

            if (!isCreator && !isCourseInstructor)
            {
                return ApiResponse.Fail<PaginatedList<ExamSubmissionDto>>(
                    "You are not authorized to view submissions for this exam.", 403);
            }
        }

        var query = _dbContext.Submissions
            .Include(s => s.Snapshots)
            .Where(s => s.ExamId == request.ExamId)
            .AsNoTracking();

        if (request.StudentId.HasValue && request.StudentId.Value != Guid.Empty)
        {
            query = query.Where(s => s.StudentId == request.StudentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<SubmissionStatus>(request.Status, true, out var parsedStatus))
            {
                query = query.Where(s => s.Status == parsedStatus);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var submissions = await query
            .OrderByDescending(s => s.StartedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (submissions.Count == 0)
        {
            var empty = new PaginatedList<ExamSubmissionDto>(new List<ExamSubmissionDto>(), totalCount, pageIndex, pageSize);
            return ApiResponse.Ok(empty);
        }

        var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
        var users = await _identityModuleApi.GetUsersByIdsAsync(studentIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id);

        var items = new List<ExamSubmissionDto>();
        foreach (var sub in submissions)
        {
            userDict.TryGetValue(sub.StudentId, out var user);

            var studentName = user?.FullName;
            if (string.IsNullOrWhiteSpace(studentName))
            {
                studentName = user?.UserName ?? "Student";
            }

            var studentEmail = user?.Email ?? string.Empty;

            var violations = sub.Violations.Select(v => new ViolationSummaryDto(
                v.Type,
                v.Reason,
                v.TimestampUtc)).ToList();

            items.Add(new ExamSubmissionDto(
                sub.Id,
                exam.Id,
                exam.Title,
                sub.StudentId,
                studentName,
                studentEmail,
                user?.Picture,
                sub.AttemptNumber,
                exam.MaxAttempts,
                sub.StartedAtUtc,
                sub.SubmittedAtUtc,
                sub.Status.ToString(),
                sub.Score,
                sub.IsPassed,
                sub.Violations.Count,
                violations,
                sub.Snapshots.Count));
        }

        var result = new PaginatedList<ExamSubmissionDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(result);
    }
}
