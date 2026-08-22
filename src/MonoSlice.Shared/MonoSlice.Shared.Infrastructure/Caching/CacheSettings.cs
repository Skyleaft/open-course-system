namespace MonoSlice.Shared.Infrastructure.Caching;

/// <summary>
/// Cache configuration from appsettings.
/// </summary>
public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Cache provider: "Memory" or "Redis"
    /// </summary>
    public string Provider { get; set; } = "Memory";

    public RedisSettings Redis { get; set; } = new();
}

public sealed class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
}
