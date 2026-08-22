using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Users.Auth;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Features.GoogleAuth;

public sealed class GoogleAuthCommandHandler : ICommandHandler<GoogleAuthCommand, ApiResponse<LoginResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheService _cacheService;
    private readonly AuthSettings _authSettings;

    public GoogleAuthCommandHandler(
        UserManager<ApplicationUser> userManager,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService,
        ICacheService cacheService,
        IOptions<AuthSettings> authSettings)
    {
        _userManager = userManager;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
        _cacheService = cacheService;
        _authSettings = authSettings.Value;
    }

    public async ValueTask<ApiResponse<LoginResponseDto>> Handle(
        GoogleAuthCommand command,
        CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(command.IdToken, cancellationToken);

        // Find user by Google login provider or Email
        var user = await _userManager.FindByLoginAsync("Google", googleUser.Subject) ??
                   await _userManager.FindByEmailAsync(googleUser.Email);

        if (user is null)
        {
            // Register new user with Guid.CreateVersion7()
            var baseUserName = !string.IsNullOrWhiteSpace(googleUser.Email)
                ? googleUser.Email.Split('@')[0]
                : $"google_user_{Guid.CreateVersion7():N}";

            var userName = baseUserName;
            var counter = 1;
            while (await _userManager.FindByNameAsync(userName) is not null)
            {
                userName = $"{baseUserName}{counter++}";
            }

            user = new ApplicationUser(userName, googleUser.Email)
            {
                Id = Guid.CreateVersion7(),
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                Picture = googleUser.Picture,
                EmailConfirmed = googleUser.EmailVerified
            };

            if (!string.IsNullOrWhiteSpace(googleUser.Name))
            {
                user.FullName = googleUser.Name;
            }

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => e.Description).ToList();
                throw new ValidationException(errors);
            }

            // Assign default role 'Student'
            await _userManager.AddToRoleAsync(user, "Student");

            // Link Google external login
            var addLoginResult = await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo("Google", googleUser.Subject, "Google"));

            if (!addLoginResult.Succeeded)
            {
                var errors = addLoginResult.Errors.Select(e => e.Description).ToList();
                throw new ValidationException(errors);
            }
        }
        else
        {
            // Ensure login provider link exists
            var logins = await _userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == googleUser.Subject))
            {
                await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo("Google", googleUser.Subject, "Google"));
            }

            if (!string.IsNullOrWhiteSpace(googleUser.Picture))
            {
                user.Picture = googleUser.Picture;
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            await _userManager.AddToRoleAsync(user, "Student");
            roles = ["Student"];
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_authSettings.RefreshTokenExpiryDays);
        user.LastSeen = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Store active session fingerprint in Redis
        var sessionKey = $"session:{user.Id}";
        await _cacheService.SetAsync(
            sessionKey,
            new { UserId = user.Id, RefreshToken = refreshToken, LoggedInAt = DateTime.UtcNow, Provider = "Google" },
            TimeSpan.FromDays(_authSettings.RefreshTokenExpiryDays),
            cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(_authSettings.AccessTokenExpiryMinutes);
        var userInfo = user.Adapt<UserInfoDto>() with { Roles = roles.ToList() };

        var responseDto = new LoginResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            userInfo);

        return ApiResponse.Ok(responseDto, "Google authentication successful.");
    }
}
