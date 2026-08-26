using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.ExamRules;
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

        ExamRuleConfig ruleConfig;
        if (command.RuleConfig != null)
        {
            ruleConfig = new ExamRuleConfig
            {
                Name = command.RuleConfig.Name,
                CanTabSwitch = command.RuleConfig.CanTabSwitch,
                MaxTabSwitchesAllowed = command.RuleConfig.MaxTabSwitchesAllowed,
                RestrictClipboardAndMouse = command.RuleConfig.RestrictClipboardAndMouse,
                ForceFullscreen = command.RuleConfig.ForceFullscreen,
                KeyboardDetection = command.RuleConfig.KeyboardDetection,
                RequireCamera = command.RuleConfig.RequireCamera,
                SnapshotIntervalSeconds = command.RuleConfig.SnapshotIntervalSeconds,
                RequireMicrophone = command.RuleConfig.RequireMicrophone,
                MaxAllowedViolations = command.RuleConfig.MaxAllowedViolations,
                AutoDisqualifyOnExceed = command.RuleConfig.AutoDisqualifyOnExceed
            };
        }
        else if (command.ExamRuleId.HasValue)
        {
            var rule = await _dbContext.ExamRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == command.ExamRuleId.Value, cancellationToken);
            ruleConfig = rule != null ? rule.ToConfig() : ExamRuleConfig.StrictProctored();
        }
        else
        {
            ruleConfig = ExamRuleConfig.StrictProctored();
        }

        var exam = QuizExam.Create(
            _currentUser.UserId.Value,
            command.Title,
            command.Description,
            command.DurationMinutes,
            command.PassingScore,
            command.ExamRuleId,
            ruleConfig,
            command.MaxAttempts,
            command.AvailableFromUtc,
            command.AvailableToUtc,
            command.ShuffleQuestions,
            command.ShuffleOptions,
            createdBy: _currentUser.UserId.Value);

        await _dbContext.Exams.AddAsync(exam, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ruleConfigDto = new ExamRuleConfigDto(
            exam.RuleConfig.Name,
            exam.RuleConfig.CanTabSwitch,
            exam.RuleConfig.MaxTabSwitchesAllowed,
            exam.RuleConfig.RestrictClipboardAndMouse,
            exam.RuleConfig.ForceFullscreen,
            exam.RuleConfig.KeyboardDetection,
            exam.RuleConfig.RequireCamera,
            exam.RuleConfig.SnapshotIntervalSeconds,
            exam.RuleConfig.RequireMicrophone,
            exam.RuleConfig.MaxAllowedViolations,
            exam.RuleConfig.AutoDisqualifyOnExceed);

        var dto = new ExamDetailDto(
            exam.Id,
            exam.InstructorId,
            exam.Title,
            exam.Description,
            exam.ExamRuleId,
            ruleConfigDto,
            exam.DurationMinutes,
            exam.PassingScore,
            exam.MaxAttempts,
            exam.AvailableFromUtc,
            exam.AvailableToUtc,
            exam.IsPublished,
            exam.ShuffleQuestions,
            exam.ShuffleOptions,
            exam.CreatedBy,
            exam.UpdatedBy,
            exam.CreatedAtUtc);

        return ApiResponse.Ok(dto, "Exam created successfully.");
    }
}
