using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.ExamRules.UpdateExamRule;

public sealed partial class UpdateExamRuleCommand : ICommand<ApiResponse<bool>>
{
    [Required]
    public Guid Id { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
    public bool CanTabSwitch { get; init; } = false;

    [Range(0, 999)]
    public int MaxTabSwitchesAllowed { get; init; } = 0;

    public bool RestrictClipboardAndMouse { get; init; } = true;
    public bool ForceFullscreen { get; init; } = true;
    public bool KeyboardDetection { get; init; } = true;
    public bool RequireCamera { get; init; } = true;

    [Range(10, 3600)]
    public int SnapshotIntervalSeconds { get; init; } = 45;

    public bool RequireMicrophone { get; init; } = false;

    [Range(1, 100)]
    public int MaxAllowedViolations { get; init; } = 3;

    public bool AutoDisqualifyOnExceed { get; init; } = true;
}
