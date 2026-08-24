using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.Login;

public sealed partial class LoginCommand : ICommand<ApiResponse<LoginResponseDto>>
{
    [Required]
    public string UserNameOrEmail { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; } = true;
}

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User);

public sealed record UserInfoDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? Picture,
    IReadOnlyList<string> Roles);
