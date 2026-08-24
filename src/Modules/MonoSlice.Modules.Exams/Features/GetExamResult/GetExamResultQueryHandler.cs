using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetExamResult;

public sealed class GetExamResultQueryHandler : IQueryHandler<GetExamResultQuery, ApiResponse<ExamResultDetailsDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetExamResultQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamResultDetailsDto>> Handle(
        GetExamResultQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == query.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), query.SubmissionId);
        }

        if (submission.StudentId != _currentUser.UserId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this exam result.");
        }

        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        var canViewExplanations = exam.Mode == QuizMode.Simulation ||
                                  submission.Status == SubmissionStatus.Completed ||
                                  submission.Status == SubmissionStatus.TimedOut;

        var resolvedQuestions = new List<(BankQuestion Question, decimal Points)>();

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
                resolvedQuestions.Add((q, section.PointsOverride ?? q.Points));
            }
        }

        var rng = new Random(submission.RandomSeed);
        if (exam.ShuffleQuestions)
        {
            resolvedQuestions = resolvedQuestions.OrderBy(_ => rng.Next()).ToList();
        }

        var questionReviews = resolvedQuestions.Select(item =>
        {
            var q = item.Question;
            var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var selectedIds = answer?.SelectedOptionIds ?? [];
            var essayText = answer?.EssayText;
            var awarded = answer?.AwardedScore;

            var options = q.Options.Select(o => new OptionReviewDto(
                o.Id,
                o.Text,
                canViewExplanations && o.IsCorrect
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
                canViewExplanations ? q.Explanation : null,
                options);
        }).ToList();

        var dto = new ExamResultDetailsDto(
            submission.Id,
            exam.Id,
            exam.Title,
            exam.Mode.ToString(),
            submission.Status.ToString(),
            submission.Score,
            submission.IsPassed,
            submission.StartedAtUtc,
            submission.SubmittedAtUtc,
            questionReviews);

        return ApiResponse.Ok(dto);
    }
}
