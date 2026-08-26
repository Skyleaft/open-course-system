using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Customization.Contracts;

public interface ICustomizationModuleApi
{
    Task<PublicCustomizationDto> GetPublicCustomizationAsync(CancellationToken cancellationToken = default);
    Task<FeatureToggleSettings> GetFeatureTogglesAsync(CancellationToken cancellationToken = default);
    Task<BrandingSettings> GetBrandingSettingsAsync(CancellationToken cancellationToken = default);
    Task<SecurityProctoringSettings> GetSecurityProctoringSettingsAsync(CancellationToken cancellationToken = default);
}

public sealed class CustomizationModuleApi : ICustomizationModuleApi
{
    private const string CacheKey = "customization:public";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public CustomizationModuleApi(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<PublicCustomizationDto> GetPublicCustomizationAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<PublicCustomizationDto>(CacheKey, cancellationToken);
        if (cached is not null) return cached;

        var settings = await _dbContext.SiteSettings.AsNoTracking().Where(s => s.IsPublic).ToListAsync(cancellationToken);
        var sections = await _dbContext.LandingSections.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.OrderIndex).ToListAsync(cancellationToken);

        var dto = new PublicCustomizationDto();
        foreach (var s in settings)
        {
            try
            {
                if (s.SettingKey == SettingKeys.BrandingGeneral)
                    dto.Branding = JsonSerializer.Deserialize<BrandingSettings>(s.ValueJson, JsonOptions) ?? new();
                else if (s.SettingKey == SettingKeys.ThemeStyling)
                    dto.Theme = JsonSerializer.Deserialize<ThemeSettings>(s.ValueJson, JsonOptions) ?? new();
                else if (s.SettingKey == SettingKeys.FeatureToggles)
                    dto.Features = JsonSerializer.Deserialize<FeatureToggleSettings>(s.ValueJson, JsonOptions) ?? new();
                else if (s.SettingKey == SettingKeys.LocalizationGeneral)
                    dto.Localization = JsonSerializer.Deserialize<LocalizationSettings>(s.ValueJson, JsonOptions) ?? new();
            }
            catch { }
        }

        dto.LandingSections = sections.Select(sec => new LandingSectionDto
        {
            Id = sec.Id,
            SectionType = sec.SectionType,
            Title = sec.Title,
            Subtitle = sec.Subtitle,
            OrderIndex = sec.OrderIndex,
            IsActive = sec.IsActive,
            ConfigJson = sec.ConfigJson
        }).ToList();

        await _cacheService.SetAsync(CacheKey, dto, TimeSpan.FromHours(1), cancellationToken);
        return dto;
    }

    public async Task<FeatureToggleSettings> GetFeatureTogglesAsync(CancellationToken cancellationToken = default)
    {
        var publicConfig = await GetPublicCustomizationAsync(cancellationToken);
        return publicConfig.Features;
    }

    public async Task<BrandingSettings> GetBrandingSettingsAsync(CancellationToken cancellationToken = default)
    {
        var publicConfig = await GetPublicCustomizationAsync(cancellationToken);
        return publicConfig.Branding;
    }

    public async Task<SecurityProctoringSettings> GetSecurityProctoringSettingsAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == SettingKeys.SecurityProctoring, cancellationToken);

        if (setting is null) return new SecurityProctoringSettings();

        try
        {
            return JsonSerializer.Deserialize<SecurityProctoringSettings>(setting.ValueJson, JsonOptions) ?? new SecurityProctoringSettings();
        }
        catch
        {
            return new SecurityProctoringSettings();
        }
    }
}
