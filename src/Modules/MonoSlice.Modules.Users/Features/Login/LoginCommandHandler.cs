using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Users.Features.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, ApiResponse<LoginResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AuthSettings _authSettings;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IOptions<AuthSettings> authSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
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
        await _userManager.UpdateAsync(user);

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
