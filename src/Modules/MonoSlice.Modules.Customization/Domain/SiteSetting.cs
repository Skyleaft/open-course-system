using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Customization.Domain;

public sealed class SiteSetting : AggregateRoot<Guid>
{
    public string Category { get; private set; } = string.Empty;
    public string SettingKey { get; private set; } = string.Empty;
    public string ValueJson { get; private set; } = "{}";
    public bool IsPublic { get; private set; }
    public string? Description { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private SiteSetting() { }

    public SiteSetting(
        Guid id,
        string category,
        string settingKey,
        string valueJson,
        bool isPublic,
        string? description = null,
        Guid? updatedBy = null) : base(id)
    {
        Category = category;
        SettingKey = settingKey;
        ValueJson = valueJson;
        IsPublic = isPublic;
        Description = description;
        UpdatedBy = updatedBy;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SiteSetting Create(
        string category,
        string settingKey,
        string valueJson,
        bool isPublic,
        string? description = null,
        Guid? createdBy = null)
    {
        return new SiteSetting(
            Guid.NewGuid(),
            category,
            settingKey,
            valueJson,
            isPublic,
            description,
            createdBy);
    }

    public void UpdateValue(string valueJson, Guid? updatedBy)
    {
        ValueJson = valueJson;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateVisibility(bool isPublic, Guid? updatedBy)
    {
        IsPublic = isPublic;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
