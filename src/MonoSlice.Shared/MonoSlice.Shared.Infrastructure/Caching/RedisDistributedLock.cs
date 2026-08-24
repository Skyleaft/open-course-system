using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Interfaces;
using StackExchange.Redis;

namespace MonoSlice.Shared.Infrastructure.Caching;

public class RedisDistributedLockHandle : IDistributedLockHandle
{
    private readonly IDatabase _db;
    private bool _disposed;

    public string ResourceKey { get; }
    public string LockToken { get; }
    public bool IsAcquired { get; private set; }

    public RedisDistributedLockHandle(IDatabase db, string resourceKey, string lockToken, bool isAcquired)
    {
        _db = db;
        ResourceKey = resourceKey;
        LockToken = lockToken;
        IsAcquired = isAcquired;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsAcquired)
        {
            await _db.LockReleaseAsync(ResourceKey, LockToken);
            IsAcquired = false;
        }

        GC.SuppressFinalize(this);
    }
}

public class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLock> _logger;

    public RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IDistributedLockHandle?> AcquireLockAsync(
        string resourceKey, 
        TimeSpan expiry, 
        TimeSpan waitTimeout = default, 
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{resourceKey}";
        var lockToken = Guid.CreateVersion7().ToString();

        var startTime = DateTime.UtcNow;
        var timeout = waitTimeout == default ? TimeSpan.Zero : waitTimeout;

        while (!ct.IsCancellationRequested)
        {
            var acquired = await db.LockTakeAsync(lockKey, lockToken, expiry);
            if (acquired)
            {
                _logger.LogDebug("Acquired distributed lock '{LockKey}' with token '{LockToken}'", lockKey, lockToken);
                return new RedisDistributedLockHandle(db, lockKey, lockToken, true);
            }

            if (DateTime.UtcNow - startTime >= timeout)
                break;

            await Task.Delay(50, ct);
        }

        _logger.LogWarning("Failed to acquire distributed lock '{LockKey}' within timeout", lockKey);
        return null;
    }

    public async Task<bool> ReleaseLockAsync(
        string resourceKey, 
        string lockToken, 
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var lockKey = resourceKey.StartsWith("lock:") ? resourceKey : $"lock:{resourceKey}";

        var released = await db.LockReleaseAsync(lockKey, lockToken);
        if (released)
        {
            _logger.LogDebug("Released distributed lock '{LockKey}'", lockKey);
        }
        else
        {
            _logger.LogWarning("Failed to release distributed lock '{LockKey}', token mismatch or expired", lockKey);
        }

        return released;
    }
}
