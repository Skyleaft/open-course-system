namespace MonoSlice.Shared.Abstractions.Contracts;

public sealed record UserContractDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    string UserName = "");

public interface IIdentityModuleApi
{
    Task<UserContractDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ValidateUserRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IReadOnlyList<UserContractDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
