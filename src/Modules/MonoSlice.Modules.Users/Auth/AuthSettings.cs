namespace MonoSlice.Modules.Users.Auth;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public string JwtSecret { get; set; } = "MonoSliceSuperSecretKey_For_Development_Only_AtLeast32BytesLong!";
    public string JwtIssuer { get; set; } = "MonoSlice";
    public string JwtAudience { get; set; } = "MonoSlice";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public bool EnableCookieAuth { get; set; } = true;
    public string CookieName { get; set; } = ".MonoSlice.Auth";
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
}
