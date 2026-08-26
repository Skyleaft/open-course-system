using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Exams.Features.StartExam;

public sealed class StartExamCommandHandler : ICommandHandler<StartExamCommand, ApiResponse<ExamAttemptDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly ICoursesModuleApi _coursesModuleApi;

    public StartExamCommandHandler(
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
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.ExamId);
        }

        if (!exam.IsPublished)
        {
            throw new BusinessRuleException("This exam is not yet published.");
        }

        // Validate course enrollment if exam is attached to a course
        var courseId = await _coursesModuleApi.GetCourseIdForExamAsync(exam.Id, cancellationToken);
        if (courseId.HasValue)
        {
            var isEnrolled = await _coursesModuleApi.IsStudentEnrolledAsync(studentId, courseId.Value, cancellationToken);
            if (!isEnrolled)
            {
                throw new BusinessRuleException("You must be enrolled in the course associated with this examination to attempt it.");
            }
        }

        // Check Exam Availability Schedule Window
        if (exam.AvailableFromUtc.HasValue && DateTime.UtcNow < exam.AvailableFromUtc.Value)
        {
            throw new BusinessRuleException($"This exam is scheduled to open at {exam.AvailableFromUtc.Value:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        if (exam.AvailableToUtc.HasValue && DateTime.UtcNow > exam.AvailableToUtc.Value)
        {
            throw new BusinessRuleException($"This exam closed at {exam.AvailableToUtc.Value:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        var totalQuestionsCount = 0;
        foreach (var section in exam.Sections)
        {
            if (section.QuestionBank is null) continue;
            var qCount = section.QuestionBank.Questions.Count;
            if (section.QuestionCount.HasValue && section.QuestionCount.Value > 0)
            {
                qCount = Math.Min(qCount, section.QuestionCount.Value);
            }
            totalQuestionsCount += qCount;
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
                var existingRuleConfigDto = new ExamRuleConfigDto(
                    existing.AppliedRules.Name,
                    existing.AppliedRules.CanTabSwitch,
                    existing.AppliedRules.MaxTabSwitchesAllowed,
                    existing.AppliedRules.RestrictClipboardAndMouse,
                    existing.AppliedRules.ForceFullscreen,
                    existing.AppliedRules.KeyboardDetection,
                    existing.AppliedRules.RequireCamera,
                    existing.AppliedRules.SnapshotIntervalSeconds,
                    existing.AppliedRules.RequireMicrophone,
                    existing.AppliedRules.MaxAllowedViolations,
                    existing.AppliedRules.AutoDisqualifyOnExceed);

                // Return existing attempt
                var existingAttempt = new ExamAttemptDto(
                    existing.Id,
                    exam.Id,
                    exam.Title,
                    exam.ExamRuleId,
                    existingRuleConfigDto,
                    existing.AttemptNumber,
                    exam.MaxAttempts,
                    existing.StartedAtUtc,
                    existing.MaxAllowedEndTimeUtc,
                    exam.AvailableToUtc,
                    existing.ActiveSessionToken,
                    totalQuestionsCount,
                    existing.DurationMinutes);

                return ApiResponse.Ok(existingAttempt, "Resuming active exam attempt.");
            }
        }

        // Check Max Attempt limits
        var completedAttemptsCount = await _dbContext.Submissions
            .CountAsync(s => s.ExamId == exam.Id && s.StudentId == studentId && s.Status != SubmissionStatus.InProgress, cancellationToken);

        if (completedAttemptsCount >= exam.MaxAttempts)
        {
            throw new BusinessRuleException($"Maximum attempt limit of {exam.MaxAttempts} reached for this exam.");
        }

        // Create new attempt with time capping against exam.AvailableToUtc
        var randomSeed = Random.Shared.Next(1, 1_000_000);
        var activeSessionToken = Guid.CreateVersion7().ToString("N");
        var attemptNumber = completedAttemptsCount + 1;

        var submission = QuizSubmission.Create(
            exam.Id,
            studentId,
            exam.DurationMinutes,
            randomSeed,
            activeSessionToken,
            exam.RuleConfig,
            attemptNumber,
            exam.AvailableToUtc);

        await _dbContext.Submissions.AddAsync(submission, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Store active exam session in Redis with TTL
        var ttl = TimeSpan.FromMinutes(exam.DurationMinutes + 15);
        await _cacheService.SetAsync($"exam_session:{submission.Id}", activeSessionToken, ttl, cancellationToken);

        var ruleConfigDto = new ExamRuleConfigDto(
            submission.AppliedRules.Name,
            submission.AppliedRules.CanTabSwitch,
            submission.AppliedRules.MaxTabSwitchesAllowed,
            submission.AppliedRules.RestrictClipboardAndMouse,
            submission.AppliedRules.ForceFullscreen,
            submission.AppliedRules.KeyboardDetection,
            submission.AppliedRules.RequireCamera,
            submission.AppliedRules.SnapshotIntervalSeconds,
            submission.AppliedRules.RequireMicrophone,
            submission.AppliedRules.MaxAllowedViolations,
            submission.AppliedRules.AutoDisqualifyOnExceed);

        var dto = new ExamAttemptDto(
            submission.Id,
            exam.Id,
            exam.Title,
            exam.ExamRuleId,
            ruleConfigDto,
            submission.AttemptNumber,
            exam.MaxAttempts,
            submission.StartedAtUtc,
            submission.MaxAllowedEndTimeUtc,
            exam.AvailableToUtc,
            submission.ActiveSessionToken,
            totalQuestionsCount,
            submission.DurationMinutes);

        return ApiResponse.Ok(dto, "Exam attempt started successfully.");
    }
}
