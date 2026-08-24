using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Users.Contracts;

public sealed class UsersModuleApi : IIdentityModuleApi, IUsersModuleApi
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
            user.FullName,
            roles.ToList(),
            isActive,
            user.UserName ?? string.Empty,
            user.Picture);
    }

    public async Task<UserContractDto?> GetUserByEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

        return new UserContractDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            roles.ToList(),
            isActive,
            user.UserName ?? string.Empty,
            user.Picture);
    }

    public async Task<bool> ValidateUserRoleAsync(
        Guid userId, 
        string role, 
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<IReadOnlyList<UserContractDto>> GetUsersByIdsAsync(
        IEnumerable<Guid> userIds, 
        CancellationToken ct = default)
    {
        var idList = userIds.ToList();
        var users = await _userManager.Users
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(ct);

        var result = new List<UserContractDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;
            result.Add(new UserContractDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                roles.ToList(),
                isActive,
                user.UserName ?? string.Empty,
                user.Picture));
        }

        return result;
    }

    public async Task<bool> IsUserActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }
}
