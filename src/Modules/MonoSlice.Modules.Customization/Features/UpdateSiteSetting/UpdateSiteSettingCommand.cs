using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Customization.Features.UpdateSiteSetting;

public sealed record UpdateSiteSettingCommand(
    string SettingKey,
    string ValueJson,
    bool? IsPublic = null) : ICommand<ApiResponse<bool>>;

public sealed class UpdateSiteSettingCommandHandler : ICommandHandler<UpdateSiteSettingCommand, ApiResponse<bool>>
{
    private const string CacheKey = "customization:public";
    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public UpdateSiteSettingCommandHandler(
        CustomizationDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        UpdateSiteSettingCommand command,
        CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SiteSettings
            .FirstOrDefaultAsync(s => s.SettingKey == command.SettingKey, cancellationToken);

        var userId = _currentUser.UserId ?? Guid.Empty;
        string? oldValueJson = null;

        if (setting is null)
        {
            // Determine category from key prefix (e.g., "branding.general" -> "Branding")
            var category = command.SettingKey.Contains('.')
                ? char.ToUpper(command.SettingKey.Split('.')[0][0]) + command.SettingKey.Split('.')[0][1..]
                : "General";

            setting = SiteSetting.Create(
                category,
                command.SettingKey,
                command.ValueJson,
                command.IsPublic ?? false,
                createdBy: userId);

            await _dbContext.SiteSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            oldValueJson = setting.ValueJson;
            setting.UpdateValue(command.ValueJson, userId);

            if (command.IsPublic.HasValue)
            {
                setting.UpdateVisibility(command.IsPublic.Value, userId);
            }
        }

        // Add audit record
        var auditLog = SettingsAuditLog.Create(
            command.SettingKey,
            oldValueJson,
            command.ValueJson,
            userId);

        await _dbContext.SettingsAuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate public settings cache
        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(true, $"Setting '{command.SettingKey}' updated successfully.");
    }
}
