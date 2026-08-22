using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Users.Auth;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly AuthSettings _authSettings;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        IOptions<AuthSettings> authSettings,
        ILogger<GoogleAuthService> logger)
    {
        _authSettings = authSettings.Value;
        _logger = logger;
    }

    public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ValidationException("Google ID Token cannot be empty.");
        }

        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrWhiteSpace(_authSettings.GoogleClientId))
            {
                validationSettings.Audience = [_authSettings.GoogleClientId];
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new ValidationException("Invalid Google token payload.");
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.Name,
                payload.GivenName,
                payload.FamilyName,
                payload.Picture,
                payload.EmailVerified);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Failed to validate Google ID Token.");
            throw new ValidationException($"Invalid Google ID Token: {ex.Message}");
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Unexpected error validating Google ID Token.");
            throw new ValidationException("An error occurred validating Google credentials.");
        }
    }
}
