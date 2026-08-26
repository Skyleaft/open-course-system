using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Customization.Features.GetPublicCustomization;

public sealed record GetPublicCustomizationQuery() : IQuery<ApiResponse<PublicCustomizationDto>>;

public sealed class GetPublicCustomizationQueryHandler : IQueryHandler<GetPublicCustomizationQuery, ApiResponse<PublicCustomizationDto>>
{
    private const string CacheKey = "customization:public";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetPublicCustomizationQueryHandler(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<PublicCustomizationDto>> Handle(
        GetPublicCustomizationQuery query,
        CancellationToken cancellationToken)
    {
        var cached = await _cacheService.GetAsync<PublicCustomizationDto>(CacheKey, cancellationToken);
        if (cached is not null)
        {
            return ApiResponse.Ok(cached, "Public customization retrieved from cache.");
        }

        var publicSettings = await _dbContext.SiteSettings
            .AsNoTracking()
            .Where(s => s.IsPublic)
            .ToListAsync(cancellationToken);

        var landingSections = await _dbContext.LandingSections
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync(cancellationToken);

        var dto = new PublicCustomizationDto();

        foreach (var setting in publicSettings)
        {
            try
            {
                switch (setting.SettingKey)
                {
                    case SettingKeys.BrandingGeneral:
                        dto.Branding = JsonSerializer.Deserialize<BrandingSettings>(setting.ValueJson, JsonOptions) ?? new();
                        break;
                    case SettingKeys.ThemeStyling:
                        dto.Theme = JsonSerializer.Deserialize<ThemeSettings>(setting.ValueJson, JsonOptions) ?? new();
                        break;
                    case SettingKeys.FeatureToggles:
                        dto.Features = JsonSerializer.Deserialize<FeatureToggleSettings>(setting.ValueJson, JsonOptions) ?? new();
                        break;
                    case SettingKeys.LocalizationGeneral:
                        dto.Localization = JsonSerializer.Deserialize<LocalizationSettings>(setting.ValueJson, JsonOptions) ?? new();
                        break;
                }
            }
            catch
            {
                // Fallback to default in case of corrupted JSON
            }
        }

        dto.LandingSections = landingSections.Select(s => new LandingSectionDto
        {
            Id = s.Id,
            SectionType = s.SectionType,
            Title = s.Title,
            Subtitle = s.Subtitle,
            OrderIndex = s.OrderIndex,
            IsActive = s.IsActive,
            ConfigJson = s.ConfigJson
        }).ToList();

        await _cacheService.SetAsync(CacheKey, dto, CacheDuration, cancellationToken);

        return ApiResponse.Ok(dto, "Public customization retrieved successfully.");
    }
}
