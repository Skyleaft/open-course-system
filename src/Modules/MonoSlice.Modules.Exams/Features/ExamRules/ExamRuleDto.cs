namespace MonoSlice.Modules.Exams.Features.ExamRules;

public sealed record ExamRuleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemPreset,
    bool CanTabSwitch,
    int MaxTabSwitchesAllowed,
    bool RestrictClipboardAndMouse,
    bool ForceFullscreen,
    bool KeyboardDetection,
    bool RequireCamera,
    int SnapshotIntervalSeconds,
    bool RequireMicrophone,
    int MaxAllowedViolations,
    bool AutoDisqualifyOnExceed,
    Guid? CreatedBy,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ExamRuleConfigDto(
    string Name,
    bool CanTabSwitch,
    int MaxTabSwitchesAllowed,
    bool RestrictClipboardAndMouse,
    bool ForceFullscreen,
    bool KeyboardDetection,
    bool RequireCamera,
    int SnapshotIntervalSeconds,
    bool RequireMicrophone,
    int MaxAllowedViolations,
    bool AutoDisqualifyOnExceed);
