using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Users.Features.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, ApiResponse<RefreshTokenResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AuthSettings _authSettings;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IOptions<AuthSettings> authSettings)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
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
        await _userManager.UpdateAsync(user);

        var expiresAt = DateTime.UtcNow.AddMinutes(_authSettings.AccessTokenExpiryMinutes);

        var responseDto = new RefreshTokenResponseDto(newAccessToken, newRefreshToken, expiresAt);
        return ApiResponse.Ok(responseDto, "Token refreshed successfully.");
    }
}
