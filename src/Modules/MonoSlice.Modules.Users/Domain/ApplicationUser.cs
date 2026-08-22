using Microsoft.AspNetCore.Identity;

namespace MonoSlice.Modules.Users.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string FullName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                return UserName ?? Email ?? string.Empty;

            return $"{FirstName} {LastName}".Trim();
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var parts = value.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            FirstName = parts[0];
            LastName = parts.Length > 1 ? parts[1] : null;
        }
    }

    public string? Picture { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
        SecurityStamp = Guid.CreateVersion7().ToString("N");
    }

    public ApplicationUser(string userName, string email) : this()
    {
        UserName = userName;
        Email = email;
    }
}
