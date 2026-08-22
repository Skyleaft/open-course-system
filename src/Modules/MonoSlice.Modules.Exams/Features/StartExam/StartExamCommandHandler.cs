using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.StartExam;

public sealed class StartExamCommandHandler : ICommandHandler<StartExamCommand, ApiResponse<ExamAttemptDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IServiceProvider _serviceProvider;

    public StartExamCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<ApiResponse<ExamAttemptDto>> Handle(
        StartExamCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to take an exam.");
        }

        var studentId = _currentUser.UserId.Value;

        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.ExamId);
        }

        if (!exam.IsPublished)
        {
            throw new BusinessRuleException("This exam is not yet published.");
        }

        // Verify course enrollment if linked to a course
        if (exam.CourseId.HasValue)
        {
            var coursesApi = _serviceProvider.GetService<ICoursesModuleApi>();
            if (coursesApi is not null)
            {
                var isEnrolled = await coursesApi.IsStudentEnrolledAsync(studentId, exam.CourseId.Value, cancellationToken);
                if (!isEnrolled)
                {
                    throw new BusinessRuleException("You must be enrolled in the course to attempt this exam.");
                }
            }
        }

        // Check if there is an existing in-progress submission
        var existing = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.ExamId == exam.Id && s.StudentId == studentId && s.Status == SubmissionStatus.InProgress, cancellationToken);

        if (existing is not null)
        {
            // If already expired
            if (DateTime.UtcNow > existing.MaxAllowedEndTimeUtc)
            {
                existing.MarkTimedOut();
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Return existing attempt
                var existingAttempt = new ExamAttemptDto(
                    existing.Id,
                    exam.Id,
                    exam.Title,
                    exam.Mode.ToString(),
                    existing.StartedAtUtc,
                    existing.MaxAllowedEndTimeUtc,
                    existing.ActiveSessionToken,
                    exam.Questions.Count,
                    exam.DurationMinutes);

                return ApiResponse.Ok(existingAttempt, "Resuming active exam attempt.");
            }
        }

        // Create new attempt
        var randomSeed = Random.Shared.Next(1, 1_000_000);
        var activeSessionToken = Guid.CreateVersion7().ToString("N");

        var submission = QuizSubmission.Create(
            exam.Id,
            studentId,
            exam.DurationMinutes,
            randomSeed,
            activeSessionToken);

        await _dbContext.Submissions.AddAsync(submission, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Store active exam session in Redis with TTL
        var ttl = TimeSpan.FromMinutes(exam.DurationMinutes + 15);
        await _cacheService.SetAsync($"exam_session:{submission.Id}", activeSessionToken, ttl, cancellationToken);

        var dto = new ExamAttemptDto(
            submission.Id,
            exam.Id,
            exam.Title,
            exam.Mode.ToString(),
            submission.StartedAtUtc,
            submission.MaxAllowedEndTimeUtc,
            submission.ActiveSessionToken,
            exam.Questions.Count,
            exam.DurationMinutes);

        return ApiResponse.Ok(dto, "Exam attempt started successfully.");
    }
}
