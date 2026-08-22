namespace MonoSlice.Shared.Abstractions.Contracts;

public interface IUsersModuleApi
{
    Task<UserContractDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserContractDto(
    Guid Id,
    string Email,
    string UserName,
    IReadOnlyList<string> Roles,
    bool IsActive);
