using Microsoft.AspNetCore.Identity;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Users.Contracts;

public sealed class UsersModuleApi : IUsersModuleApi
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersModuleApi(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserContractDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

        return new UserContractDto(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles.ToList(),
            isActive);
    }

    public async Task<bool> IsUserActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }
}
