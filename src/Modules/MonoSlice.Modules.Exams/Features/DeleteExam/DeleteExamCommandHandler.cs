using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.Features.DeleteExam;

public sealed class DeleteExamCommandHandler : ICommandHandler<DeleteExamCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DeleteExamCommandHandler> _logger;

    public DeleteExamCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<DeleteExamCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<bool>> Handle(DeleteExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException("Quiz exam not found.");
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != exam.InstructorId)
        {
            throw new BusinessRuleException("You do not have permission to delete this examination.");
        }

        var submissions = await _dbContext.Submissions
            .Where(s => s.ExamId == exam.Id)
            .ToListAsync(cancellationToken);

        if (submissions.Count > 0)
        {
            _dbContext.Submissions.RemoveRange(submissions);
        }

        _dbContext.Exams.Remove(exam);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Exam {ExamId} deleted by user {UserId}", exam.Id, _currentUser.UserId);

        // Async Cache and cross-module event dispatching
        await _cacheService.RemoveAsync($"exam:{exam.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"exam:questions:{exam.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"exam:list:*", cancellationToken);

        await _eventBus.PublishAsync(
            new ExamDeletedIntegrationEvent(exam.Id, exam.InstructorId, exam.CourseId),
            cancellationToken);

        return ApiResponse.Ok(true, "Exam and all associated data deleted successfully.");
    }
}
