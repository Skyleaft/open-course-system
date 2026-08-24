using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Features.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, ApiResponse<RefreshTokenResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheService _cacheService;
    private readonly AuthSettings _authSettings;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ICacheService cacheService,
        IOptions<AuthSettings> authSettings)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _cacheService = cacheService;
        _authSettings = authSettings.Value;
    }

    public async ValueTask<ApiResponse<RefreshTokenResponseDto>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
        {
            throw new ValidationException("Invalid access token.");
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new ValidationException("Invalid token claims.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.RefreshToken != command.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Invalid or expired refresh token.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpiryDays);
        user.LastSeen = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Update Redis session fingerprint
        var sessionKey = $"session:{user.Id}";
        await _cacheService.SetAsync(sessionKey, new { UserId = user.Id, RefreshToken = newRefreshToken, RefreshedAt = DateTime.UtcNow }, TimeSpan.FromDays(_authSettings.RefreshTokenExpiryDays), cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(_authSettings.AccessTokenExpiryMinutes);

        var responseDto = new RefreshTokenResponseDto(newAccessToken, newRefreshToken, expiresAt);
        return ApiResponse.Ok(responseDto, "Token refreshed successfully.");
    }
}
