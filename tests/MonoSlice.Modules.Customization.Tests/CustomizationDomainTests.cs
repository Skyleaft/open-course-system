using MonoSlice.Modules.Customization.Domain;
using Xunit;

namespace MonoSlice.Modules.Customization.Tests;

public sealed class CustomizationDomainTests
{
    [Fact]
    public void SiteSetting_Create_ShouldInitializePropertiesCorrectly()
    {
        // Arrange & Act
        var setting = SiteSetting.Create(
            category: SettingCategories.Branding,
            settingKey: SettingKeys.BrandingGeneral,
            valueJson: "{\"siteName\":\"Test LMS\"}",
            isPublic: true,
            description: "Test description");

        // Assert
        Assert.NotEqual(Guid.Empty, setting.Id);
        Assert.Equal(SettingCategories.Branding, setting.Category);
        Assert.Equal(SettingKeys.BrandingGeneral, setting.SettingKey);
        Assert.Equal("{\"siteName\":\"Test LMS\"}", setting.ValueJson);
        Assert.True(setting.IsPublic);
        Assert.Equal("Test description", setting.Description);
    }

    [Fact]
    public void SiteSetting_UpdateValue_ShouldUpdatePayloadAndTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = SiteSetting.Create(
            SettingCategories.Theme,
            SettingKeys.ThemeStyling,
            "{\"primaryColor\":\"#6366f1\"}",
            true);

        // Act
        setting.UpdateValue("{\"primaryColor\":\"#10b981\"}", userId);

        // Assert
        Assert.Equal("{\"primaryColor\":\"#10b981\"}", setting.ValueJson);
        Assert.Equal(userId, setting.UpdatedBy);
    }

    [Fact]
    public void LandingSection_Create_And_Update_ShouldWorkCorrectly()
    {
        // Arrange
        var section = LandingSection.Create(
            "Hero",
            "Hero Title",
            "Hero Subtitle",
            1,
            true,
            "{\"cta\":\"Explore\"}");

        // Assert creation
        Assert.NotEqual(Guid.Empty, section.Id);
        Assert.Equal("Hero", section.SectionType);
        Assert.True(section.IsActive);
        Assert.Equal(1, section.OrderIndex);

        // Act - Update
        section.Update("New Title", "New Subtitle", 2, false, "{\"cta\":\"Join\"}");

        // Assert update
        Assert.Equal("New Title", section.Title);
        Assert.Equal("New Subtitle", section.Subtitle);
        Assert.Equal(2, section.OrderIndex);
        Assert.False(section.IsActive);
    }

    [Fact]
    public void SettingsAuditLog_Create_ShouldCaptureDiff()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var audit = SettingsAuditLog.Create(
            SettingKeys.BrandingGeneral,
            "{\"old\":\"val\"}",
            "{\"new\":\"val\"}",
            userId,
            "127.0.0.1");

        // Assert
        Assert.NotEqual(Guid.Empty, audit.Id);
        Assert.Equal(SettingKeys.BrandingGeneral, audit.SettingKey);
        Assert.Equal("{\"old\":\"val\"}", audit.OldValueJson);
        Assert.Equal("{\"new\":\"val\"}", audit.NewValueJson);
        Assert.Equal(userId, audit.ChangedBy);
        Assert.Equal("127.0.0.1", audit.IpAddress);
    }
}
