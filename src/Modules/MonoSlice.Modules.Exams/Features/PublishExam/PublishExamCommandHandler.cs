using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.PublishExam;

public sealed class PublishExamCommandHandler : ICommandHandler<PublishExamCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;

    public PublishExamCommandHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse> Handle(
        PublishExamCommand command,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.Id);
        }

        if (command.Publish)
        {
            exam.Publish();
        }
        else
        {
            exam.Unpublish();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var state = command.Publish ? "published" : "unpublished";
        return ApiResponse.Ok($"Exam '{exam.Title}' {state} successfully.");
    }
}
