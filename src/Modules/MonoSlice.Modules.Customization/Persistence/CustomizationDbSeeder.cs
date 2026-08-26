using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;

namespace MonoSlice.Modules.Customization.Persistence;

public static class CustomizationDbSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static async Task SeedDefaultsAsync(
        CustomizationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.SiteSettings.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seeding default Site Settings for Open Course System...");

            var defaultBranding = new BrandingSettings();
            var defaultTheme = new ThemeSettings();
            var defaultFeatures = new FeatureToggleSettings();
            var defaultLocalization = new LocalizationSettings();
            var defaultSecurity = new SecurityProctoringSettings();

            var settings = new List<SiteSetting>
            {
                SiteSetting.Create(
                    SettingCategories.Branding,
                    SettingKeys.BrandingGeneral,
                    JsonSerializer.Serialize(defaultBranding, JsonOptions),
                    isPublic: true,
                    description: "Global platform branding, title, logos, and copyright information."),

                SiteSetting.Create(
                    SettingCategories.Theme,
                    SettingKeys.ThemeStyling,
                    JsonSerializer.Serialize(defaultTheme, JsonOptions),
                    isPublic: true,
                    description: "Visual design tokens, OKLCH color palettes, fonts, and glassmorphism styling."),

                SiteSetting.Create(
                    SettingCategories.Features,
                    SettingKeys.FeatureToggles,
                    JsonSerializer.Serialize(defaultFeatures, JsonOptions),
                    isPublic: true,
                    description: "Platform feature switchboard, registration rules, and maintenance toggles."),

                SiteSetting.Create(
                    SettingCategories.Localization,
                    SettingKeys.LocalizationGeneral,
                    JsonSerializer.Serialize(defaultLocalization, JsonOptions),
                    isPublic: true,
                    description: "Localization defaults, timezone, date formats, and custom terminology."),

                SiteSetting.Create(
                    SettingCategories.Security,
                    SettingKeys.SecurityProctoring,
                    JsonSerializer.Serialize(defaultSecurity, JsonOptions),
                    isPublic: false,
                    description: "Strict anti-cheat proctoring rules, camera snapshots, and violation thresholds.")
            };

            await dbContext.SiteSettings.AddRangeAsync(settings, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.LandingSections.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seeding default Landing Sections for Open Course System...");

            var heroConfig = JsonSerializer.Serialize(new
            {
                badgeText = "Next-Gen Open Learning Engine",
                headlineGradient = "Elevate Your Learning & Examination Integrity",
                subheadline = "An extensible, customizable LMS equipped with real-time browser proctoring, Fisher-Yates PRNG question shuffling, and high-concurrency exam streaming.",
                primaryCtaText = "Explore Catalog",
                primaryCtaLink = "/courses",
                secondaryCtaText = "Sign In Portal",
                secondaryCtaLink = "/login"
            }, JsonOptions);

            var statsConfig = JsonSerializer.Serialize(new
            {
                stats = new[]
                {
                    new { label = "Exam Integrity", value = "99.9%", description = "Realtime anti-cheat proctoring" },
                    new { label = "Zero-DB Latency", value = "< 2ms", description = "In-memory Redis answer buffering" },
                    new { label = "Active Courses", value = "500+", description = "Modular curriculum builder" },
                    new { label = "Verified Students", value = "25k+", description = "Cryptographic SHA-256 certs" }
                }
            }, JsonOptions);

            var featuresConfig = JsonSerializer.Serialize(new
            {
                features = new[]
                {
                    new { title = "Dual-Mode Exams", description = "Practice freely in Simulation mode or enforce high-stakes RealExam anti-cheat lockdowns.", icon = "ShieldCheck" },
                    new { title = "Fisher-Yates Shuffling", description = "Deterministic PRNG shuffling guarantees each student receives randomized question sequences.", icon = "Shuffle" },
                    new { title = "MinIO S3 Presigned Uploads", description = "Zero backend server upload bottleneck with direct presigned S3 candidate snapshot streaming.", icon = "HardDrive" },
                    new { title = "Zero-DB Redis Autosave", description = "High-concurrency answer autosave ensures instant persistence with auto-recovery on disconnect.", icon = "Zap" }
                }
            }, JsonOptions);

            var sections = new List<LandingSection>
            {
                LandingSection.Create("Hero", "Hero Banner", "Main landing hero section", 1, true, heroConfig),
                LandingSection.Create("StatsCounter", "Platform Metrics", "Key performance indicators", 2, true, statsConfig),
                LandingSection.Create("FeaturedCourses", "Featured Curriculum", "Highlighted course catalog", 3, true, "{}"),
                LandingSection.Create("FeaturesGrid", "Engine Capabilities", "Architecture and security highlights", 4, true, featuresConfig)
            };

            await dbContext.LandingSections.AddRangeAsync(sections, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
