using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.AssignRole;

public sealed partial class AssignRoleCommand : ICommand<ApiResponse>
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string RoleName { get; init; } = string.Empty;
}
