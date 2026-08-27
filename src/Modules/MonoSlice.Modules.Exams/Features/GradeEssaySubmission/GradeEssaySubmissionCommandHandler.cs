using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Modules.Exams.Features.GetExamResult;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GradeEssaySubmission;

public sealed class GradeEssaySubmissionCommandHandler : ICommandHandler<GradeEssaySubmissionCommand, ApiResponse<ExamResultDetailsDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GradeEssaySubmissionCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamResultDetailsDto>> Handle(
        GradeEssaySubmissionCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var isInstructorOrAdmin = _currentUser.IsInRole("Instructor") || _currentUser.IsInRole("Admin");
        if (!isInstructorOrAdmin)
        {
            throw new UnauthorizedAccessException("Only instructors or admins can evaluate and grade essay answers.");
        }

        var submission = await _dbContext.Submissions
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        var exam = await _dbContext.Exams
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        // Resolve exam questions and calculate total maximum points
        decimal totalMaxPoints = 0m;
        var questionMap = new Dictionary<Guid, (BankQuestion Question, decimal Points)>();
        var resolvedQuestionsList = new List<(BankQuestion Question, decimal Points)>();

        foreach (var section in exam.Sections.OrderBy(s => s.OrderIndex))
        {
            if (section.QuestionBank is null) continue;
            var questions = section.QuestionBank.Questions.OrderBy(q => q.OrderIndex).ToList();
            if (section.QuestionCount.HasValue && section.QuestionCount.Value > 0)
            {
                questions = questions.Take(section.QuestionCount.Value).ToList();
            }

            foreach (var q in questions)
            {
                var pts = section.PointsOverride ?? q.Points;
                questionMap[q.Id] = (q, pts);
                resolvedQuestionsList.Add((q, pts));
                totalMaxPoints += pts;
            }
        }

        // Apply manual essay grades
        foreach (var grade in command.Grades)
        {
            var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == grade.QuestionId);
            if (answer is null)
            {
                answer = StudentAnswer.Create(submission.Id, grade.QuestionId, null, null);
                await _dbContext.StudentAnswers.AddAsync(answer, cancellationToken);
            }

            if (questionMap.TryGetValue(grade.QuestionId, out var qInfo))
            {
                var validScore = Math.Clamp(grade.Score, 0m, qInfo.Points);
                answer.SetAwardedScore(validScore);
            }
            else
            {
                answer.SetAwardedScore(Math.Max(0m, grade.Score));
            }
        }

        // Recalculate total earned points across all answers
        decimal totalEarnedPoints = submission.Answers.Sum(a => a.AwardedScore ?? 0m);
        decimal calculatedPercentage = totalMaxPoints > 0m
            ? Math.Clamp((totalEarnedPoints / totalMaxPoints) * 100m, 0m, 100m)
            : 0m;

        submission.UpdateManualGrade(Math.Round(calculatedPercentage, 2), exam.PassingScore);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Build updated ExamResultDetailsDto
        var rng = new Random(submission.RandomSeed);
        if (exam.ShuffleQuestions)
        {
            resolvedQuestionsList = resolvedQuestionsList.OrderBy(_ => rng.Next()).ToList();
        }

        var questionReviews = resolvedQuestionsList.Select(item =>
        {
            var q = item.Question;
            var ans = submission.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var selectedIds = ans?.SelectedOptionIds ?? [];
            var essayText = ans?.EssayText;
            var awarded = ans?.AwardedScore;

            var options = q.Options.Select(o => new OptionReviewDto(
                o.Id,
                o.Text,
                true // Instructor can always view correct options
            )).ToList();

            if (exam.ShuffleOptions)
            {
                options = options.OrderBy(_ => rng.Next()).ToList();
            }

            return new QuestionReviewDto(
                q.Id,
                q.QuestionText,
                q.Type.ToString(),
                item.Points,
                awarded,
                selectedIds,
                essayText,
                q.Explanation,
                options);
        }).ToList();

        var appliedRulesDto = new ExamRuleConfigDto(
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

        var dto = new ExamResultDetailsDto(
            submission.Id,
            exam.Id,
            exam.Title,
            exam.ExamRuleId,
            appliedRulesDto,
            submission.Status.ToString(),
            submission.Score,
            submission.IsPassed,
            submission.StartedAtUtc,
            submission.SubmittedAtUtc,
            questionReviews);

        return ApiResponse.Ok(dto, "Essay answers evaluated and submission final grade calculated successfully.");
    }
}
