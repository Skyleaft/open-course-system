using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        var isFirstUser = false;
        try
        {
            isFirstUser = _userManager.Users is null || !await _userManager.Users.AnyAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Fallback for non-EF Core async query providers (e.g. Unit test mocks)
            isFirstUser = _userManager.Users is null || !_userManager.Users.Any();
        }

        var userName = !string.IsNullOrWhiteSpace(command.UserName)
            ? command.UserName
            : command.Email.Split('@')[0];

        var user = new ApplicationUser(userName, command.Email)
        {
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        if (!string.IsNullOrWhiteSpace(command.FullName) && string.IsNullOrWhiteSpace(command.FirstName))
        {
            user.FullName = command.FullName;
        }

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ValidationException(errors);
        }

        // Assign 'Admin' role to the first user created in the system, otherwise default to 'Student'
        var assignedRole = isFirstUser ? "Admin" : "Student";
        await _userManager.AddToRoleAsync(user, assignedRole);

        var responseDto = user.Adapt<UserResponseDto>() with { Roles = [assignedRole] };

        return ApiResponse.Ok(responseDto, "User registered successfully.");
    }
}
