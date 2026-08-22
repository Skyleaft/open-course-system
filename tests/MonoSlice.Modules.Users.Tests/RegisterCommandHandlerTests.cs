using Microsoft.AspNetCore.Identity;
using NSubstitute;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Modules.Users.Features.Register;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Users.Tests;

public class RegisterCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
        _handler = new RegisterCommandHandler(_userManager);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenUserAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "existing@example.com",
            UserName = "existinguser",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(command.Email)
            .Returns(new ApplicationUser("existinguser", command.Email));

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRegistrationIsValid()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "newuser@example.com",
            UserName = "newuser",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userManager.FindByEmailAsync(command.Email)
            .Returns((ApplicationUser?)null);

        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), command.Password)
            .Returns(IdentityResult.Success);

        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(command.Email, result.Data.Email);
        Assert.Equal(command.UserName, result.Data.UserName);
        Assert.Contains("User", result.Data.Roles);
    }
}
