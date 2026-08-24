using Mapster;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.CreateExam;

public sealed class CreateExamCommandHandler : ICommandHandler<CreateExamCommand, ApiResponse<ExamDetailDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateExamCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamDetailDto>> Handle(
        CreateExamCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to create exams.");
        }

        var exam = QuizExam.Create(
            _currentUser.UserId.Value,
            command.Title,
            command.Description,
            command.Mode,
            command.DurationMinutes,
            command.PassingScore,
            command.MaxAllowedViolations,
            command.MaxAttempts,
            command.AvailableFromUtc,
            command.AvailableToUtc,
            command.ShuffleQuestions,
            command.ShuffleOptions,
            createdBy: _currentUser.UserId.Value);

        await _dbContext.Exams.AddAsync(exam, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = exam.Adapt<ExamDetailDto>() with
        {
            Mode = exam.Mode.ToString()
        };

        return ApiResponse.Ok(dto, "Exam created successfully.");
    }
}
