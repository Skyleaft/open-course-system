namespace MonoSlice.Modules.Users.Auth;

public sealed record GoogleUserInfo(
    string Subject,
    string Email,
    string? Name,
    string? GivenName,
    string? FamilyName,
    string? Picture,
    bool EmailVerified);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
