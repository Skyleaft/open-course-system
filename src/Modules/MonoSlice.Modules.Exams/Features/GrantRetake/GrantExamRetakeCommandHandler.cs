using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GrantRetake;

public sealed class GrantExamRetakeCommandHandler : ICommandHandler<GrantExamRetakeCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly ICoursesModuleApi _coursesModuleApi;

    public GrantExamRetakeCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        ICoursesModuleApi coursesModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
        _coursesModuleApi = coursesModuleApi;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        GrantExamRetakeCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var exam = await _dbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.ExamId);
        }

        var isAdmin = _currentUser.Roles.Contains("Admin");
        var isInstructor = _currentUser.Roles.Contains("Instructor");

        if (!isAdmin)
        {
            // Verify if instructor created the exam or teaches the course
            var isCreator = exam.CreatedBy == _currentUser.UserId.Value;
            var courseId = await _coursesModuleApi.GetCourseIdForExamAsync(exam.Id, cancellationToken);
            var isCourseInstructor = false;

            if (courseId.HasValue)
            {
                // In courses module, instructors can manage attached exams
                isCourseInstructor = isInstructor;
            }

            if (!isCreator && !isCourseInstructor)
            {
                return ApiResponse.Fail<bool>("You are not authorized to manage attempts for this exam.", 403);
            }
        }

        var submissions = await _dbContext.Submissions
            .Include(s => s.Answers)
            .Include(s => s.Snapshots)
            .Where(s => s.ExamId == command.ExamId && s.StudentId == command.StudentId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);

        if (submissions.Count == 0)
        {
            return ApiResponse.Ok(true, "Student has not started any attempts yet. Retake is already available.");
        }

        // Clean up Redis session and answers cache for all student submissions
        foreach (var sub in submissions)
        {
            await _cacheService.RemoveAsync($"exam_session:{sub.Id}", cancellationToken);
            await _cacheService.RemoveAsync($"exam_answers:{sub.Id}", cancellationToken);
        }

        // Remove the latest blocked/failed/disqualified submission record to free up an attempt slot
        var latestSubmission = submissions.First();
        _dbContext.Submissions.Remove(latestSubmission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, $"Retake permission granted for student. Previous attempt (#{latestSubmission.AttemptNumber}) was reset.");
    }
}
