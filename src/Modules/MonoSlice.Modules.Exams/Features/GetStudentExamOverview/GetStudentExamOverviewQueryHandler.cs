using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetStudentExamOverview;

public sealed class GetStudentExamOverviewQueryHandler : IQueryHandler<GetStudentExamOverviewQuery, ApiResponse<StudentExamOverviewDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetStudentExamOverviewQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<StudentExamOverviewDto>> Handle(
        GetStudentExamOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .FirstOrDefaultAsync(e => e.Id == query.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), query.ExamId);
        }

        var totalQuestionsCount = 0;
        foreach (var sec in exam.Sections)
        {
            if (sec.QuestionBank is null) continue;
            var qCount = sec.QuestionBank.Questions.Count;
            if (sec.QuestionCount.HasValue && sec.QuestionCount.Value > 0)
            {
                qCount = Math.Min(qCount, sec.QuestionCount.Value);
            }
            totalQuestionsCount += qCount;
        }

        var completedAttemptsCount = 0;
        var remainingAttempts = exam.MaxAttempts;
        decimal? bestScore = null;
        var isPassed = false;
        var hasActiveSession = false;
        Guid? activeSubmissionId = null;

        if (_currentUser.IsAuthenticated && _currentUser.UserId.HasValue)
        {
            var studentId = _currentUser.UserId.Value;
            var studentSubmissions = await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ExamId == exam.Id && s.StudentId == studentId)
                .ToListAsync(cancellationToken);

            var completedAttempts = studentSubmissions
                .Where(s => s.Status != SubmissionStatus.InProgress)
                .ToList();

            completedAttemptsCount = completedAttempts.Count;
            remainingAttempts = Math.Max(0, exam.MaxAttempts - completedAttemptsCount);
            bestScore = completedAttempts.Count > 0 ? completedAttempts.Max(s => s.Score) : null;
            isPassed = completedAttempts.Any(s => s.Status == SubmissionStatus.Completed && s.Score >= exam.PassingScore);

            var activeSession = studentSubmissions
                .FirstOrDefault(s => s.Status == SubmissionStatus.InProgress && s.MaxAllowedEndTimeUtc > DateTime.UtcNow);

            if (activeSession is not null)
            {
                hasActiveSession = true;
                activeSubmissionId = activeSession.Id;
            }
        }

        var ruleConfigDto = new ExamRuleConfigDto(
            exam.RuleConfig.Name,
            exam.RuleConfig.CanTabSwitch,
            exam.RuleConfig.MaxTabSwitchesAllowed,
            exam.RuleConfig.RestrictClipboardAndMouse,
            exam.RuleConfig.ForceFullscreen,
            exam.RuleConfig.KeyboardDetection,
            exam.RuleConfig.RequireCamera,
            exam.RuleConfig.SnapshotIntervalSeconds,
            exam.RuleConfig.RequireMicrophone,
            exam.RuleConfig.MaxAllowedViolations,
            exam.RuleConfig.AutoDisqualifyOnExceed);

        var dto = new StudentExamOverviewDto(
            exam.Id,
            exam.Title,
            exam.Description,
            exam.ExamRuleId,
            ruleConfigDto,
            exam.DurationMinutes,
            exam.PassingScore,
            exam.MaxAttempts,
            exam.AvailableFromUtc,
            exam.AvailableToUtc,
            exam.IsPublished,
            totalQuestionsCount,
            exam.Sections.Count,
            completedAttemptsCount,
            remainingAttempts,
            bestScore,
            isPassed,
            hasActiveSession,
            activeSubmissionId
        );

        return ApiResponse.Ok(dto);
    }
}
