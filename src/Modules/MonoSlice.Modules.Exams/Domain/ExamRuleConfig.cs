namespace MonoSlice.Modules.Exams.Domain;

public sealed record ExamRuleConfig
{
    public string Name { get; init; } = "Custom Rule";
    public bool CanTabSwitch { get; init; } = false;
    public int MaxTabSwitchesAllowed { get; init; } = 0;
    public bool RestrictClipboardAndMouse { get; init; } = true;
    public bool ForceFullscreen { get; init; } = true;
    public bool KeyboardDetection { get; init; } = true;
    public bool RequireCamera { get; init; } = true;
    public int SnapshotIntervalSeconds { get; init; } = 45;
    public bool RequireMicrophone { get; init; } = false;
    public int MaxAllowedViolations { get; init; } = 3;
    public bool AutoDisqualifyOnExceed { get; init; } = true;

    public static ExamRuleConfig StrictProctored() => new()
    {
        Name = "Strict Proctored",
        CanTabSwitch = false,
        MaxTabSwitchesAllowed = 0,
        RestrictClipboardAndMouse = true,
        ForceFullscreen = true,
        KeyboardDetection = true,
        RequireCamera = true,
        SnapshotIntervalSeconds = 45,
        RequireMicrophone = true,
        MaxAllowedViolations = 3,
        AutoDisqualifyOnExceed = true
    };

    public static ExamRuleConfig StandardQuiz() => new()
    {
        Name = "Standard Quiz",
        CanTabSwitch = false,
        MaxTabSwitchesAllowed = 3,
        RestrictClipboardAndMouse = true,
        ForceFullscreen = true,
        KeyboardDetection = true,
        RequireCamera = false,
        SnapshotIntervalSeconds = 45,
        RequireMicrophone = false,
        MaxAllowedViolations = 3,
        AutoDisqualifyOnExceed = true
    };

    public static ExamRuleConfig OpenBook() => new()
    {
        Name = "Open Book / Coding",
        CanTabSwitch = true,
        MaxTabSwitchesAllowed = 999,
        RestrictClipboardAndMouse = false,
        ForceFullscreen = false,
        KeyboardDetection = false,
        RequireCamera = true,
        SnapshotIntervalSeconds = 60,
        RequireMicrophone = false,
        MaxAllowedViolations = 5,
        AutoDisqualifyOnExceed = false
    };

    public static ExamRuleConfig Practice() => new()
    {
        Name = "Practice Mode",
        CanTabSwitch = true,
        MaxTabSwitchesAllowed = 999,
        RestrictClipboardAndMouse = false,
        ForceFullscreen = false,
        KeyboardDetection = false,
        RequireCamera = false,
        SnapshotIntervalSeconds = 60,
        RequireMicrophone = false,
        MaxAllowedViolations = 999,
        AutoDisqualifyOnExceed = false
    };
}
