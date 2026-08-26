using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ExamRules.DeleteExamRule;

public sealed class DeleteExamRuleCommandHandler : ICommandHandler<DeleteExamRuleCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;

    public DeleteExamRuleCommandHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<bool>> Handle(DeleteExamRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.ExamRules
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (rule is null)
        {
            return ApiResponse.Fail<bool>("Exam rule not found.", 404);
        }

        if (rule.IsSystemPreset)
        {
            return ApiResponse.Fail<bool>("System preset rules cannot be deleted.", 400);
        }

        _dbContext.ExamRules.Remove(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, "Exam rule deleted successfully.");
    }
}
