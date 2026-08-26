using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class ExamRule : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemPreset { get; private set; }
    public bool CanTabSwitch { get; private set; }
    public int MaxTabSwitchesAllowed { get; private set; }
    public bool RestrictClipboardAndMouse { get; private set; }
    public bool ForceFullscreen { get; private set; }
    public bool KeyboardDetection { get; private set; }
    public bool RequireCamera { get; private set; }
    public int SnapshotIntervalSeconds { get; private set; } = 45;
    public bool RequireMicrophone { get; private set; }
    public int MaxAllowedViolations { get; private set; } = 3;
    public bool AutoDisqualifyOnExceed { get; private set; } = true;
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    private ExamRule() : base(Guid.CreateVersion7()) { }

    public static ExamRule Create(
        string name,
        string? description = null,
        bool isSystemPreset = false,
        bool canTabSwitch = false,
        int maxTabSwitchesAllowed = 0,
        bool restrictClipboardAndMouse = true,
        bool forceFullscreen = true,
        bool keyboardDetection = true,
        bool requireCamera = true,
        int snapshotIntervalSeconds = 45,
        bool requireMicrophone = false,
        int maxAllowedViolations = 3,
        bool autoDisqualifyOnExceed = true,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Exam rule name cannot be empty.");
        }

        if (maxAllowedViolations <= 0)
        {
            throw new BusinessRuleException("Max allowed violations must be at least 1.");
        }

        if (snapshotIntervalSeconds < 10)
        {
            throw new BusinessRuleException("Snapshot interval must be at least 10 seconds.");
        }

        return new ExamRule
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystemPreset = isSystemPreset,
            CanTabSwitch = canTabSwitch,
            MaxTabSwitchesAllowed = Math.Max(0, maxTabSwitchesAllowed),
            RestrictClipboardAndMouse = restrictClipboardAndMouse,
            ForceFullscreen = forceFullscreen,
            KeyboardDetection = keyboardDetection,
            RequireCamera = requireCamera,
            SnapshotIntervalSeconds = snapshotIntervalSeconds,
            RequireMicrophone = requireMicrophone,
            MaxAllowedViolations = maxAllowedViolations,
            AutoDisqualifyOnExceed = autoDisqualifyOnExceed,
            CreatedBy = createdBy,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        bool canTabSwitch,
        int maxTabSwitchesAllowed,
        bool restrictClipboardAndMouse,
        bool forceFullscreen,
        bool keyboardDetection,
        bool requireCamera,
        int snapshotIntervalSeconds,
        bool requireMicrophone,
        int maxAllowedViolations,
        bool autoDisqualifyOnExceed)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Exam rule name cannot be empty.");
        }

        if (maxAllowedViolations <= 0)
        {
            throw new BusinessRuleException("Max allowed violations must be at least 1.");
        }

        if (snapshotIntervalSeconds < 10)
        {
            throw new BusinessRuleException("Snapshot interval must be at least 10 seconds.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        CanTabSwitch = canTabSwitch;
        MaxTabSwitchesAllowed = Math.Max(0, maxTabSwitchesAllowed);
        RestrictClipboardAndMouse = restrictClipboardAndMouse;
        ForceFullscreen = forceFullscreen;
        KeyboardDetection = keyboardDetection;
        RequireCamera = requireCamera;
        SnapshotIntervalSeconds = snapshotIntervalSeconds;
        RequireMicrophone = requireMicrophone;
        MaxAllowedViolations = maxAllowedViolations;
        AutoDisqualifyOnExceed = autoDisqualifyOnExceed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public ExamRuleConfig ToConfig() => new()
    {
        Name = Name,
        CanTabSwitch = CanTabSwitch,
        MaxTabSwitchesAllowed = MaxTabSwitchesAllowed,
        RestrictClipboardAndMouse = RestrictClipboardAndMouse,
        ForceFullscreen = ForceFullscreen,
        KeyboardDetection = KeyboardDetection,
        RequireCamera = RequireCamera,
        SnapshotIntervalSeconds = SnapshotIntervalSeconds,
        RequireMicrophone = RequireMicrophone,
        MaxAllowedViolations = MaxAllowedViolations,
        AutoDisqualifyOnExceed = AutoDisqualifyOnExceed
    };
}
