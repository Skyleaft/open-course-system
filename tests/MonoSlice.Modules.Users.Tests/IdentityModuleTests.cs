using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Contracts;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Modules.Users.Features.Logout;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Users.Tests;

public class IdentityModuleTests
{
    [Fact]
    public async Task Login_ShouldUpdateLastSeen_AndStoreSessionInCache()
    {
        // Arrange
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            userManager, contextAccessor, claimsFactory, null, null, null, null);
        var jwtTokenService = Substitute.For<IJwtTokenService>();
        var cacheService = Substitute.For<ICacheService>();
        var authOptions = Options.Create(new AuthSettings
        {
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
            JwtSecret = "a_very_secret_key_that_is_at_least_32_characters_long!"
        });

        var user = new ApplicationUser("teststudent", "student@example.com")
        {
            FirstName = "Test",
            LastName = "Student"
        };

        userManager.FindByEmailAsync("student@example.com").Returns(user);
        signInManager.CheckPasswordSignInAsync(user, "Password123!", true)
            .Returns(SignInResult.Success);
        userManager.GetRolesAsync(user).Returns(["Student"]);
        jwtTokenService.GenerateAccessToken(user, Arg.Any<IList<string>>()).Returns("access_token_123");
        jwtTokenService.GenerateRefreshToken().Returns("refresh_token_123");
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var handler = new LoginCommandHandler(userManager, signInManager, jwtTokenService, cacheService, authOptions);

        // Act
        var command = new LoginCommand
        {
            UserNameOrEmail = "student@example.com",
            Password = "Password123!"
        };
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("access_token_123", result.Data.AccessToken);
        Assert.NotNull(user.LastSeen);
        await cacheService.Received(1).SetAsync(
            $"session:{user.Id}",
            Arg.Any<object>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_ShouldClearSessionFromCache()
    {
        // Arrange
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            userManager, contextAccessor, claimsFactory, null, null, null, null);
        var currentUser = Substitute.For<ICurrentUser>();
        var cacheService = Substitute.For<ICacheService>();

        var userId = Guid.NewGuid();
        currentUser.UserId.Returns(userId);

        var user = new ApplicationUser("teststudent", "student@example.com") { RefreshToken = "old_refresh" };
        userManager.FindByIdAsync(userId.ToString()).Returns(user);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var handler = new LogoutCommandHandler(userManager, signInManager, currentUser, cacheService);

        // Act
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(user.RefreshToken);
        await cacheService.Received(1).RemoveAsync($"session:{userId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IdentityModuleApi_ValidateRole_ShouldReturnTrue_WhenUserHasRole()
    {
        // Arrange
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        var userId = Guid.NewGuid();
        var user = new ApplicationUser("testproctor", "proctor@example.com");
        userManager.FindByIdAsync(userId.ToString()).Returns(user);
        userManager.IsInRoleAsync(user, "Proctor").Returns(true);

        var api = new UsersModuleApi(userManager);

        // Act
        var hasRole = await api.ValidateUserRoleAsync(userId, "Proctor");

        // Assert
        Assert.True(hasRole);
    }
}
