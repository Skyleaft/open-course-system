using Microsoft.AspNetCore.Identity;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Features.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, ApiResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public LogoutCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var targetUserId = command.UserId ?? _currentUser.UserId;
        if (targetUserId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(targetUserId.Value.ToString());
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            // Invalidate Redis session guard
            var sessionKey = $"session:{targetUserId.Value}";
            await _cacheService.RemoveAsync(sessionKey, cancellationToken);
        }

        await _signInManager.SignOutAsync();
        return ApiResponse.Ok("User logged out successfully.");
    }
}
