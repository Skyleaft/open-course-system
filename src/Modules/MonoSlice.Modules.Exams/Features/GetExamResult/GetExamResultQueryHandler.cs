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
            .Include(e => e.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        var canViewExplanations = exam.Mode == QuizMode.Simulation || submission.Status == SubmissionStatus.Completed;

        var questionReviews = exam.Questions.Select(q =>
        {
            var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var selectedIds = answer?.SelectedOptionIds ?? [];
            var essayText = answer?.EssayText;
            var awarded = answer?.AwardedScore;

            var options = q.Options.Select(o => new OptionReviewDto(
                o.Id,
                o.Text,
                canViewExplanations && o.IsCorrect
            )).ToList();

            return new QuestionReviewDto(
                q.Id,
                q.QuestionText,
                q.Type.ToString(),
                q.Points,
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
