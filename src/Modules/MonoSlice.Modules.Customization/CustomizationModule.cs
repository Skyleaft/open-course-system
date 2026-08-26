using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Customization.Contracts;
using MonoSlice.Modules.Customization.Features.BatchUpdateSettings;
using MonoSlice.Modules.Customization.Features.GetAdminCustomization;
using MonoSlice.Modules.Customization.Features.GetPublicCustomization;
using MonoSlice.Modules.Customization.Features.ManageLandingSections;
using MonoSlice.Modules.Customization.Features.UpdateSiteSetting;
using MonoSlice.Modules.Customization.Features.UploadBrandAssetPresign;
using MonoSlice.Modules.Customization.Persistence;

namespace MonoSlice.Modules.Customization;

public static class CustomizationModule
{
    public static IServiceCollection AddCustomizationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CustomizationDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CustomizationDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<CustomizationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CustomizationDbContext.DefaultSchema);
                });
            });
        }

        // Register module contract API
        services.AddScoped<ICustomizationModuleApi, CustomizationModuleApi>();

        return services;
    }

    public static IEndpointRouteBuilder MapCustomizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customization")
            .WithTags("Customization");

        // Public Queries
        group.MapGetPublicCustomizationEndpoint();

        // Admin Customization Queries & Updates
        group.MapGetAdminCustomizationEndpoint();
        group.MapUpdateSiteSettingEndpoint();
        group.MapBatchUpdateSettingsEndpoint();

        // Landing Sections Management
        group.MapLandingSectionEndpoints();

        // Brand Asset MinIO Upload Presign
        group.MapUploadBrandAssetPresignEndpoint();

        return app;
    }
}
