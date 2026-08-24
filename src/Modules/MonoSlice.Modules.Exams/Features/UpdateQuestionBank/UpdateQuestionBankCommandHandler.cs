using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestionBank;

public sealed class UpdateQuestionBankCommandHandler : ICommandHandler<UpdateQuestionBankCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateQuestionBankCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<bool>> Handle(UpdateQuestionBankCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return ApiResponse.Fail<bool>("Unauthorized.", 401);
        }

        var bank = await _dbContext.QuestionBanks
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (bank is null)
        {
            return ApiResponse.Fail<bool>("Question Bank package not found.", 404);
        }

        bank.Update(
            updatedBy: _currentUser.UserId.Value,
            title: command.Title,
            description: command.Description,
            category: command.Category,
            tags: command.Tags);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok(true, "Question Bank package updated successfully.");
    }
}
