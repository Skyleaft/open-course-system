using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.ExamRules.CreateExamRule;

public sealed class CreateExamRuleCommandHandler : ICommandHandler<CreateExamRuleCommand, ApiResponse<Guid>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateExamRuleCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<Guid>> Handle(CreateExamRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = ExamRule.Create(
            command.Name,
            command.Description,
            isSystemPreset: false,
            canTabSwitch: command.CanTabSwitch,
            maxTabSwitchesAllowed: command.MaxTabSwitchesAllowed,
            restrictClipboardAndMouse: command.RestrictClipboardAndMouse,
            forceFullscreen: command.ForceFullscreen,
            keyboardDetection: command.KeyboardDetection,
            requireCamera: command.RequireCamera,
            snapshotIntervalSeconds: command.SnapshotIntervalSeconds,
            requireMicrophone: command.RequireMicrophone,
            maxAllowedViolations: command.MaxAllowedViolations,
            autoDisqualifyOnExceed: command.AutoDisqualifyOnExceed,
            createdBy: _currentUser.UserId);

        _dbContext.ExamRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(rule.Id, "Exam rule created successfully.", 201);
    }
}
