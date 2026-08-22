using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Users.Features.Login;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.GoogleAuth;

public sealed record GoogleAuthCommand : ICommand<ApiResponse<LoginResponseDto>>
{
    [Required]
    public string IdToken { get; init; } = string.Empty;
}
