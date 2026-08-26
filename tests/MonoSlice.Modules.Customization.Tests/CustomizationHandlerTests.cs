using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Features.BatchUpdateSettings;
using MonoSlice.Modules.Customization.Features.GetPublicCustomization;
using MonoSlice.Modules.Customization.Features.ManageLandingSections;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Customization.Tests;

public sealed class CustomizationHandlerTests
{
    private static CustomizationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CustomizationDbContext(options);
    }

    [Fact]
    public async Task GetPublicCustomization_ShouldReturnDefaultFallback_WhenDatabaseIsEmpty()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cacheService = Substitute.For<ICacheService>();
        cacheService.GetAsync<PublicCustomizationDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PublicCustomizationDto?>(null));

        var handler = new GetPublicCustomizationQueryHandler(db, cacheService);

        // Act
        var result = await handler.Handle(new GetPublicCustomizationQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Open Course System", result.Data.Branding.SiteName);
        Assert.Equal("dark", result.Data.Theme.DefaultTheme);
        Assert.True(result.Data.Features.EnablePublicCatalog);
    }

    [Fact]
    public async Task BatchUpdateSettings_ShouldPersistAndInvalidateCache()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cacheService = Substitute.For<ICacheService>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentUserId = Guid.NewGuid();
        currentUser.UserId.Returns(currentUserId);

        var handler = new BatchUpdateSettingsCommandHandler(db, cacheService, currentUser);

        var newBranding = new BrandingSettings { SiteName = "Custom University LMS" };
        var newTheme = new ThemeSettings { DefaultTheme = "light", PrimaryColor = "#10b981" };

        var command = new BatchUpdateSettingsCommand(Branding: newBranding, Theme: newTheme);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        await cacheService.Received(1).RemoveAsync("customization:public", Arg.Any<CancellationToken>());

        var persistedBranding = await db.SiteSettings.FirstOrDefaultAsync(s => s.SettingKey == SettingKeys.BrandingGeneral);
        Assert.NotNull(persistedBranding);
        Assert.Contains("Custom University LMS", persistedBranding.ValueJson);

        var auditLogs = await db.SettingsAuditLogs.ToListAsync();
        Assert.Equal(2, auditLogs.Count);
    }

    [Fact]
    public async Task CreateLandingSection_ShouldAddSectionAndInvalidateCache()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var cacheService = Substitute.For<ICacheService>();
        var handler = new CreateLandingSectionCommandHandler(db, cacheService);

        var command = new CreateLandingSectionCommand(
            SectionType: "Testimonials",
            Title: "What Students Say",
            Subtitle: "Real reviews from real learners",
            OrderIndex: 5,
            IsActive: true,
            ConfigJson: "{\"testimonials\":[]}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
        await cacheService.Received(1).RemoveAsync("customization:public", Arg.Any<CancellationToken>());

        var section = await db.LandingSections.FindAsync(result.Data);
        Assert.NotNull(section);
        Assert.Equal("Testimonials", section.SectionType);
    }
}
