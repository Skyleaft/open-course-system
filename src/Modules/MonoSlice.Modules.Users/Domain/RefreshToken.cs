using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Users.Domain;

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ReplacedByToken { get; set; }

    public bool IsActive => !IsRevoked && ExpiresAtUtc > DateTime.UtcNow;

    public RefreshToken()
    {
        Id = Guid.CreateVersion7();
    }

    public RefreshToken(Guid userId, string token, DateTime expiresAtUtc) : this()
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
    }
}
