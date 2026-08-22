using Microsoft.AspNetCore.Identity;

namespace MonoSlice.Modules.Users.Domain;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole()
    {
        Id = Guid.CreateVersion7();
    }

    public ApplicationRole(string roleName, string? description = null) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
        Description = description;
    }
}
