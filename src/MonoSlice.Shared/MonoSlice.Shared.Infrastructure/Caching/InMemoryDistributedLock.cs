using System.Collections.Concurrent;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Shared.Infrastructure.Caching;

public class InMemoryDistributedLock : IDistributedLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IDistributedLockHandle?> AcquireLockAsync(
        string resourceKey, 
        TimeSpan expiry, 
        TimeSpan waitTimeout = default, 
        CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
        var timeout = waitTimeout == default ? TimeSpan.FromSeconds(5) : waitTimeout;

        var acquired = await semaphore.WaitAsync(timeout, ct);
        if (!acquired) return null;

        var token = Guid.NewGuid().ToString();
        return new InMemoryLockHandle(resourceKey, token, semaphore);
    }

    public Task<bool> ReleaseLockAsync(
        string resourceKey, 
        string lockToken, 
        CancellationToken ct = default)
    {
        if (_locks.TryGetValue(resourceKey, out var semaphore))
        {
            if (semaphore.CurrentCount == 0)
            {
                semaphore.Release();
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    private sealed class InMemoryLockHandle : IDistributedLockHandle
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public string ResourceKey { get; }
        public string LockToken { get; }
        public bool IsAcquired { get; private set; }

        public InMemoryLockHandle(string resourceKey, string lockToken, SemaphoreSlim semaphore)
        {
            ResourceKey = resourceKey;
            LockToken = lockToken;
            _semaphore = semaphore;
            IsAcquired = true;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;

            if (IsAcquired)
            {
                _semaphore.Release();
                IsAcquired = false;
            }

            return ValueTask.CompletedTask;
        }
    }
}
