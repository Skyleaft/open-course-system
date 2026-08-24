namespace MonoSlice.Shared.Abstractions.Interfaces;

/// <summary>
/// Unit of work for transactional persistence.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
