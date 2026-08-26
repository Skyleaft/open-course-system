namespace MonoSlice.Modules.Customization.Domain.Models;

public sealed class BrandingSettings
{
    public string SiteName { get; set; } = "Open Course System";
    public string Tagline { get; set; } = "Customizable LMS & Online Examination Platform";
    public string Description { get; set; } = "Next-generation open education platform with realtime anti-cheat proctoring and interactive learning.";
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string FooterCopyright { get; set; } = "© 2026 Open Course System. All rights reserved.";
    public string? ContactEmail { get; set; } = "contact@opencourse.io";
    public string? PrivacyPolicyUrl { get; set; }
    public string? TermsOfServiceUrl { get; set; }
    public List<SocialLink> SocialLinks { get; set; } = [];
}

public sealed class SocialLink
{
    public string Platform { get; set; } = string.Empty; // GitHub, Twitter, LinkedIn, YouTube, Discord
    public string Url { get; set; } = string.Empty;
}

public sealed class ThemeSettings
{
    public string DefaultTheme { get; set; } = "dark"; // dark, light, luxury, synthwave, etc.
    public bool AllowThemeSwitch { get; set; } = true;
    public string PrimaryColor { get; set; } = "#6366f1"; // Indigo
    public string SecondaryColor { get; set; } = "#a855f7"; // Purple
    public string AccentColor { get; set; } = "#ec4899"; // Pink
    public string NeutralColor { get; set; } = "#1f2937";
    public string FontFamily { get; set; } = "Outfit";
    public bool Glassmorphism { get; set; } = true;
    public string BorderRadius { get; set; } = "0.75rem";
    public string? CustomCss { get; set; }
}

public sealed class FeatureToggleSettings
{
    public bool EnablePublicCatalog { get; set; } = true;
    public bool EnableRegistration { get; set; } = true;
    public string? RegistrationDomainRestriction { get; set; } // e.g. "university.edu;campus.ac.id"
    public bool EnablePayments { get; set; } = false; // Mock payments or payment gateway
    public string DefaultCurrency { get; set; } = "IDR";
    public bool EnableCertificates { get; set; } = true;
    public bool EnableDiscussions { get; set; } = true;
    public bool EnableAnnouncements { get; set; } = true;
    public bool MaintenanceMode { get; set; } = false;
    public string? MaintenanceMessage { get; set; }
}

public sealed class LocalizationSettings
{
    public string DefaultLanguage { get; set; } = "id";
    public List<string> SupportedLanguages { get; set; } = ["id", "en"];
    public string Timezone { get; set; } = "Asia/Jakarta";
    public string DateFormat { get; set; } = "DD MMMM YYYY";
    public Dictionary<string, string> CustomTerms { get; set; } = new()
    {
        ["Course"] = "Kursus",
        ["Exam"] = "Ujian",
        ["Instructor"] = "Instruktur",
        ["Student"] = "Peserta"
    };
}

public sealed class SecurityProctoringSettings
{
    public int DefaultMaxViolations { get; set; } = 3;
    public int SnapshotIntervalSeconds { get; set; } = 45;
    public bool EnforceFullscreen { get; set; } = true;
    public bool EnforceCamera { get; set; } = true;
    public bool EnforceMicrophone { get; set; } = true;
    public bool BlockClipboard { get; set; } = true;
    public bool BlockInspectElement { get; set; } = true;
}

public sealed class PublicCustomizationDto
{
    public BrandingSettings Branding { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
    public FeatureToggleSettings Features { get; set; } = new();
    public LocalizationSettings Localization { get; set; } = new();
    public List<LandingSectionDto> LandingSections { get; set; } = [];
}

public sealed class AdminCustomizationDto
{
    public BrandingSettings Branding { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
    public FeatureToggleSettings Features { get; set; } = new();
    public LocalizationSettings Localization { get; set; } = new();
    public SecurityProctoringSettings Security { get; set; } = new();
    public List<LandingSectionDto> LandingSections { get; set; } = [];
}

public sealed class LandingSectionDto
{
    public Guid Id { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
    public string ConfigJson { get; set; } = "{}";
}
