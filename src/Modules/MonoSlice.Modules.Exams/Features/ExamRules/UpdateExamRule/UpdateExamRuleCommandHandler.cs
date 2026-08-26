using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ExamRules.UpdateExamRule;

public sealed class UpdateExamRuleCommandHandler : ICommandHandler<UpdateExamRuleCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;

    public UpdateExamRuleCommandHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<bool>> Handle(UpdateExamRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.ExamRules
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (rule is null)
        {
            return ApiResponse.Fail<bool>("Exam rule not found.", 404);
        }

        if (rule.IsSystemPreset)
        {
            return ApiResponse.Fail<bool>("System preset rules cannot be modified directly. Create a custom rule instead.", 400);
        }

        rule.Update(
            command.Name,
            command.Description,
            command.CanTabSwitch,
            command.MaxTabSwitchesAllowed,
            command.RestrictClipboardAndMouse,
            command.ForceFullscreen,
            command.KeyboardDetection,
            command.RequireCamera,
            command.SnapshotIntervalSeconds,
            command.RequireMicrophone,
            command.MaxAllowedViolations,
            command.AutoDisqualifyOnExceed);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, "Exam rule updated successfully.");
    }
}
