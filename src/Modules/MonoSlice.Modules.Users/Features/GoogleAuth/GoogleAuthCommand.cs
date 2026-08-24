using Sannr;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.GoogleAuth;

public sealed partial class GoogleAuthCommand : ICommand<ApiResponse<LoginResponseDto>>
{
    [Required]
    public string IdToken { get; init; } = string.Empty;
}
