using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ExamRules.GetExamRule;

public sealed class GetExamRuleQueryHandler : IQueryHandler<GetExamRuleQuery, ApiResponse<ExamRuleDto>>
{
    private readonly ExamsDbContext _dbContext;

    public GetExamRuleQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<ExamRuleDto>> Handle(GetExamRuleQuery query, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.ExamRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (rule is null)
        {
            return ApiResponse.Fail<ExamRuleDto>("Exam rule not found.", 404);
        }

        var dto = new ExamRuleDto(
            rule.Id,
            rule.Name,
            rule.Description,
            rule.IsSystemPreset,
            rule.CanTabSwitch,
            rule.MaxTabSwitchesAllowed,
            rule.RestrictClipboardAndMouse,
            rule.ForceFullscreen,
            rule.KeyboardDetection,
            rule.RequireCamera,
            rule.SnapshotIntervalSeconds,
            rule.RequireMicrophone,
            rule.MaxAllowedViolations,
            rule.AutoDisqualifyOnExceed,
            rule.CreatedBy,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);

        return ApiResponse.Ok(dto, "Exam rule retrieved successfully.");
    }
}
