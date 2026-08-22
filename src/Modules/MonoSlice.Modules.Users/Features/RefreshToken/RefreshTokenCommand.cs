using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.RefreshToken;

public sealed record RefreshTokenCommand : ICommand<ApiResponse<RefreshTokenResponseDto>>
{
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record RefreshTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
