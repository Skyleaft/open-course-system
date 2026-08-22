using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Messaging;
using StackExchange.Redis;

namespace MonoSlice.Shared.Infrastructure.Messaging;

public class RedisEventStreamPublisher : IEventStreamPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisEventStreamPublisher> _logger;
    private static readonly ActivitySource ActivitySource = new("MonoSlice.EventStream");

    public RedisEventStreamPublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisEventStreamPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string> PublishAsync<T>(
        string streamKey, 
        T payload, 
        int? maxLen = 100000, 
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity($"PublishStream {streamKey}", ActivityKind.Producer);

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(payload);
        var messageId = Guid.NewGuid().ToString();

        var entries = new List<NameValueEntry>
        {
            new("id", messageId),
            new("type", typeof(T).Name),
            new("payload", json),
            new("published_at_utc", DateTime.UtcNow.ToString("O"))
        };

        // Inject W3C TraceContext if available
        if (activity != null)
        {
            entries.Add(new("traceparent", activity.Id ?? string.Empty));
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                entries.Add(new("tracestate", activity.TraceStateString));
            }
        }

        var redisResult = await db.StreamAddAsync(
            streamKey,
            entries.ToArray(),
            maxLength: maxLen,
            useApproximateMaxLength: true);

        var streamMessageId = redisResult.ToString();
        _logger.LogInformation("Published message '{MessageId}' (Stream ID: {StreamMessageId}) to stream '{StreamKey}'",
            messageId, streamMessageId, streamKey);

        return streamMessageId;
    }

    public async Task<string> PublishRawAsync(
        string streamKey, 
        IDictionary<string, string> entries, 
        int? maxLen = 100000, 
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity($"PublishRawStream {streamKey}", ActivityKind.Producer);

        var db = _redis.GetDatabase();
        var nameValueEntries = entries.Select(kvp => new NameValueEntry(kvp.Key, kvp.Value)).ToList();

        if (activity != null && !entries.ContainsKey("traceparent"))
        {
            nameValueEntries.Add(new("traceparent", activity.Id ?? string.Empty));
        }

        var redisResult = await db.StreamAddAsync(
            streamKey,
            nameValueEntries.ToArray(),
            maxLength: maxLen,
            useApproximateMaxLength: true);

        var streamMessageId = redisResult.ToString();
        _logger.LogInformation("Published raw entries to stream '{StreamKey}' (Stream ID: {StreamMessageId})",
            streamKey, streamMessageId);

        return streamMessageId;
    }
}
