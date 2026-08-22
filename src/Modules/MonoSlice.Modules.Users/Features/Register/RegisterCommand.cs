using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Users.Features.Register;

public sealed record RegisterCommand : ICommand<ApiResponse<UserResponseDto>>
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public sealed record UserResponseDto(
    Guid Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
