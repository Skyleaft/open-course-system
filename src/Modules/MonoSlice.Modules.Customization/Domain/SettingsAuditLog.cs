using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Customization.Domain;

public sealed class SettingsAuditLog : Entity<Guid>
{
    public string SettingKey { get; private set; } = string.Empty;
    public string? OldValueJson { get; private set; }
    public string NewValueJson { get; private set; } = "{}";
    public Guid ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; } = DateTime.UtcNow;
    public string? IpAddress { get; private set; }

    private SettingsAuditLog() { }

    public SettingsAuditLog(
        Guid id,
        string settingKey,
        string? oldValueJson,
        string newValueJson,
        Guid changedBy,
        string? ipAddress = null) : base(id)
    {
        SettingKey = settingKey;
        OldValueJson = oldValueJson;
        NewValueJson = newValueJson;
        ChangedBy = changedBy;
        ChangedAtUtc = DateTime.UtcNow;
        IpAddress = ipAddress;
    }

    public static SettingsAuditLog Create(
        string settingKey,
        string? oldValueJson,
        string newValueJson,
        Guid changedBy,
        string? ipAddress = null)
    {
        return new SettingsAuditLog(
            Guid.NewGuid(),
            settingKey,
            oldValueJson,
            newValueJson,
            changedBy,
            ipAddress);
    }
}
