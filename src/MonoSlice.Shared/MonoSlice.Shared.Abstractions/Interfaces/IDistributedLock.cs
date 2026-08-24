namespace MonoSlice.Shared.Abstractions.Interfaces;

public interface IDistributedLockHandle : IAsyncDisposable
{
    string ResourceKey { get; }
    string LockToken { get; }
    bool IsAcquired { get; }
}

public interface IDistributedLock
{
    Task<IDistributedLockHandle?> AcquireLockAsync(
        string resourceKey, 
        TimeSpan expiry, 
        TimeSpan waitTimeout = default, 
        CancellationToken ct = default);

    Task<bool> ReleaseLockAsync(
        string resourceKey, 
        string lockToken, 
        CancellationToken ct = default);
}
