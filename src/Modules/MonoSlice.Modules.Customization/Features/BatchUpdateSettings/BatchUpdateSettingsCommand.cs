using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Customization.Features.BatchUpdateSettings;

public sealed record BatchUpdateSettingsCommand(
    BrandingSettings? Branding = null,
    ThemeSettings? Theme = null,
    FeatureToggleSettings? Features = null,
    LocalizationSettings? Localization = null,
    SecurityProctoringSettings? Security = null) : ICommand<ApiResponse<bool>>;

public sealed class BatchUpdateSettingsCommandHandler : ICommandHandler<BatchUpdateSettingsCommand, ApiResponse<bool>>
{
    private const string CacheKey = "customization:public";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public BatchUpdateSettingsCommandHandler(
        CustomizationDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        BatchUpdateSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var updates = new List<(string Category, string Key, string Json, bool IsPublic)>();

        if (command.Branding is not null)
            updates.Add((SettingCategories.Branding, SettingKeys.BrandingGeneral, JsonSerializer.Serialize(command.Branding, JsonOptions), true));

        if (command.Theme is not null)
            updates.Add((SettingCategories.Theme, SettingKeys.ThemeStyling, JsonSerializer.Serialize(command.Theme, JsonOptions), true));

        if (command.Features is not null)
            updates.Add((SettingCategories.Features, SettingKeys.FeatureToggles, JsonSerializer.Serialize(command.Features, JsonOptions), true));

        if (command.Localization is not null)
            updates.Add((SettingCategories.Localization, SettingKeys.LocalizationGeneral, JsonSerializer.Serialize(command.Localization, JsonOptions), true));

        if (command.Security is not null)
            updates.Add((SettingCategories.Security, SettingKeys.SecurityProctoring, JsonSerializer.Serialize(command.Security, JsonOptions), false));

        foreach (var (category, key, json, isPublic) in updates)
        {
            var setting = await _dbContext.SiteSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);

            string? oldValueJson = null;
            if (setting is null)
            {
                setting = SiteSetting.Create(category, key, json, isPublic, createdBy: userId);
                await _dbContext.SiteSettings.AddAsync(setting, cancellationToken);
            }
            else
            {
                oldValueJson = setting.ValueJson;
                setting.UpdateValue(json, userId);
                setting.UpdateVisibility(isPublic, userId);
            }

            var audit = SettingsAuditLog.Create(key, oldValueJson, json, userId);
            await _dbContext.SettingsAuditLogs.AddAsync(audit, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(true, "Customization settings batch updated successfully.");
    }
}
