using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.Logout;

public sealed partial class LogoutCommand : ICommand<ApiResponse>
{
    public Guid? UserId { get; init; }
}
