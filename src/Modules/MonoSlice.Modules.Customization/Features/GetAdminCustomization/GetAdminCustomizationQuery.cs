using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Customization.Features.GetAdminCustomization;

public sealed record GetAdminCustomizationQuery() : IQuery<ApiResponse<AdminCustomizationDto>>;

public sealed class GetAdminCustomizationQueryHandler : IQueryHandler<GetAdminCustomizationQuery, ApiResponse<AdminCustomizationDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CustomizationDbContext _dbContext;

    public GetAdminCustomizationQueryHandler(CustomizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<AdminCustomizationDto>> Handle(
        GetAdminCustomizationQuery query,
        CancellationToken cancellationToken)
    {
        var allSettings = await _dbContext.SiteSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var landingSections = await _dbContext.LandingSections
            .AsNoTracking()
            .OrderBy(s => s.OrderIndex)
            .ToListAsync(cancellationToken);

        var dto = new AdminCustomizationDto();

        foreach (var setting in allSettings)
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
                    case SettingKeys.SecurityProctoring:
                        dto.Security = JsonSerializer.Deserialize<SecurityProctoringSettings>(setting.ValueJson, JsonOptions) ?? new();
                        break;
                }
            }
            catch
            {
                // Ignore parse errors, keep default
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

        return ApiResponse.Ok(dto, "Admin customization retrieved successfully.");
    }
}
