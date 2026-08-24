using Microsoft.AspNetCore.Identity;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Users.Features.AssignRole;

public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, ApiResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AssignRoleCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async ValueTask<ApiResponse> Handle(
        AssignRoleCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            throw new NotFoundException(nameof(ApplicationUser), command.UserId);
        }

        if (!await _roleManager.RoleExistsAsync(command.RoleName))
        {
            throw new NotFoundException(nameof(ApplicationRole), command.RoleName);
        }

        var result = await _userManager.AddToRoleAsync(user, command.RoleName);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ValidationException(errors);
        }

        return ApiResponse.Ok($"Role '{command.RoleName}' assigned to user '{user.UserName}' successfully.");
    }
}
