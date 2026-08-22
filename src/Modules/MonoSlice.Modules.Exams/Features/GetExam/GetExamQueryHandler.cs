using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.GetExam;

public sealed class GetExamQueryHandler : IQueryHandler<GetExamQuery, ApiResponse<ExamFullDetailDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetExamQueryHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamFullDetailDto>> Handle(
        GetExamQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), query.Id);
        }

        var isInstructor = _currentUser.IsAuthenticated &&
            (_currentUser.UserId == exam.InstructorId || _currentUser.Roles.Contains("Admin") || _currentUser.Roles.Contains("Instructor"));

        var questionDtos = exam.Questions.Select(q => new QuestionResultDto(
            q.Id,
            q.ExamId,
            q.QuestionText,
            q.Type.ToString(),
            q.Points,
            q.OrderIndex,
            isInstructor ? q.Explanation : null,
            q.Options.Select(o => new QuestionOptionDto(
                o.Id,
                o.Text,
                isInstructor && o.IsCorrect
            )).ToList()
        )).ToList();

        var dto = new ExamFullDetailDto(
            exam.Id,
            exam.CourseId,
            exam.InstructorId,
            exam.Title,
            exam.Description,
            exam.Mode.ToString(),
            exam.DurationMinutes,
            exam.PassingScore,
            exam.MaxAllowedViolations,
            exam.IsPublished,
            exam.ShuffleQuestions,
            exam.ShuffleOptions,
            exam.CreatedAtUtc,
            questionDtos);

        return ApiResponse.Ok(dto);
    }
}
