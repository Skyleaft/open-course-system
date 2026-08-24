using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestionBank;

public sealed class DeleteQuestionBankCommandHandler : ICommandHandler<DeleteQuestionBankCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteQuestionBankCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<bool>> Handle(DeleteQuestionBankCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiResponse.Fail<bool>("Unauthorized.", 401);
        }

        var bank = await _dbContext.QuestionBanks
            .Include(b => b.Questions)
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

        if (bank is null)
        {
            return ApiResponse.Fail<bool>("Question Bank package not found.", 404);
        }

        // Check if referenced by active exam sections
        var isReferenced = await _dbContext.Sections
            .AnyAsync(s => s.QuestionBankId == command.Id, cancellationToken);

        if (isReferenced)
        {
            return ApiResponse.Fail<bool>("Cannot delete this Question Bank because it is linked to one or more exam sections.", 400);
        }

        _dbContext.QuestionBanks.Remove(bank);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, "Question Bank package deleted successfully.");
    }
}
