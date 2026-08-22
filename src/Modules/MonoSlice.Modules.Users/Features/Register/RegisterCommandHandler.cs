using Mapster;
using Microsoft.AspNetCore.Identity;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Users.Features.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, ApiResponse<UserResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async ValueTask<ApiResponse<UserResponseDto>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
        {
            throw new BusinessRuleException($"User with email '{command.Email}' already exists.");
        }

        var user = new ApplicationUser(command.UserName, command.Email)
        {
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, "User");

        var responseDto = user.Adapt<UserResponseDto>() with { Roles = ["User"] };
        return ApiResponse.Ok(responseDto, "User registered successfully.");
    }
}
