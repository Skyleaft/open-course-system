namespace MonoSlice.Shared.Abstractions.Contracts;

public interface IUsersModuleApi
{
    Task<UserContractDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}
