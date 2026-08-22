using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Features.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, ApiResponse<LoginResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheService _cacheService;
    private readonly AuthSettings _authSettings;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ICacheService cacheService,
        IOptions<AuthSettings> authSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _cacheService = cacheService;
        _authSettings = authSettings.Value;
    }

    public async ValueTask<ApiResponse<LoginResponseDto>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.UserNameOrEmail) ??
                   await _userManager.FindByNameAsync(command.UserNameOrEmail);

        if (user is null)
        {
            throw new ValidationException("Invalid username or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                throw new BusinessRuleException("User account is locked out due to multiple failed login attempts.");
            }

            throw new ValidationException("Invalid username or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpiryDays);
        user.LastSeen = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Store active session fingerprint in Redis
        var sessionKey = $"session:{user.Id}";
        await _cacheService.SetAsync(sessionKey, new { UserId = user.Id, RefreshToken = refreshToken, LoggedInAt = DateTime.UtcNow }, TimeSpan.FromDays(_authSettings.RefreshTokenExpiryDays), cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(_authSettings.AccessTokenExpiryMinutes);
        var userInfo = user.Adapt<UserInfoDto>() with { Roles = roles.ToList() };

        var responseDto = new LoginResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            userInfo);

        return ApiResponse.Ok(responseDto, "Login successful.");
    }
}
