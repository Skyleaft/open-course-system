using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Features.SaveAnswer;
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
    private readonly ICacheService _cacheService;

    public GetExamQuestionsQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
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

        // Retrieve any previously buffered answers from Redis to restore student state
        var cachedAnswers = await _cacheService.GetAsync<Dictionary<Guid, CachedAnswerDto>>(
            $"exam_answers:{submission.Id}", cancellationToken) ?? [];

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

            // Populate previously saved answers if student is reconnecting/resuming
            cachedAnswers.TryGetValue(q.Id, out var savedAnswer);

            questionDtos.Add(new StudentQuestionDto(
                q.Id,
                q.QuestionText,
                q.Type.ToString(),
                q.Points,
                displayOrder++,
                savedAnswer?.SelectedOptionIds,
                savedAnswer?.EssayText,
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
