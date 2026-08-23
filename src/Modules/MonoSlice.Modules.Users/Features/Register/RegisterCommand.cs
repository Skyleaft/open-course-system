using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.Register;

public sealed partial class RegisterCommand : ICommand<ApiResponse<UserResponseDto>>
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    public string? UserName { get; init; }

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    public string? FullName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public sealed record UserResponseDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? FirstName,
    string? LastName,
    string? Picture,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
