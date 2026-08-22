using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetExamQuestions;

public sealed class GetExamQuestionsQueryHandler : IQueryHandler<GetExamQuestionsQuery, ApiResponse<StudentExamPaperDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetExamQuestionsQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<StudentExamPaperDto>> Handle(
        GetExamQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), query.SubmissionId);
        }

        if (submission.StudentId != _currentUser.UserId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this exam attempt.");
        }

        if (submission.Status != SubmissionStatus.InProgress)
        {
            throw new BusinessRuleException($"Exam attempt is {submission.Status}.");
        }

        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        // Apply Fisher-Yates shuffle deterministically
        var questions = exam.Questions.ToList();
        if (exam.ShuffleQuestions)
        {
            questions = ExamShuffler.Shuffle(questions, submission.RandomSeed);
        }
        else
        {
            questions = questions.OrderBy(q => q.OrderIndex).ToList();
        }

        var displayOrder = 1;
        var questionDtos = new List<StudentQuestionDto>();

        foreach (var q in questions)
        {
            var options = q.Options.ToList();
            if (exam.ShuffleOptions && options.Count > 0)
            {
                options = ExamShuffler.Shuffle(options, submission.RandomSeed + q.OrderIndex);
            }

            var optionDtos = options.Select(o => new StudentOptionDto(o.Id, o.Text)).ToList();

            questionDtos.Add(new StudentQuestionDto(
                q.Id,
                q.QuestionText,
                q.Type.ToString(),
                q.Points,
                displayOrder++,
                optionDtos));
        }

        var paperDto = new StudentExamPaperDto(
            submission.Id,
            exam.Id,
            exam.Title,
            exam.Mode.ToString(),
            submission.StartedAtUtc,
            submission.MaxAllowedEndTimeUtc,
            questionDtos);

        return ApiResponse.Ok(paperDto);
    }
}
