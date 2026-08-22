using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Shared.Infrastructure.Messaging;

public class InMemoryEventStreamPublisher : IEventStreamPublisher
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _streams = new();
    private readonly ILogger<InMemoryEventStreamPublisher> _logger;

    public InMemoryEventStreamPublisher(ILogger<InMemoryEventStreamPublisher> logger)
    {
        _logger = logger;
    }

    public Task<string> PublishAsync<T>(
        string streamKey, 
        T payload, 
        int? maxLen = 100000, 
        CancellationToken ct = default)
    {
        var queue = _streams.GetOrAdd(streamKey, _ => new ConcurrentQueue<string>());
        var json = JsonSerializer.Serialize(payload);
        queue.Enqueue(json);

        var id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-0";
        _logger.LogInformation("In-memory stream '{StreamKey}' appended entry {Id}", streamKey, id);
        return Task.FromResult(id);
    }

    public Task<string> PublishRawAsync(
        string streamKey, 
        IDictionary<string, string> entries, 
        int? maxLen = 100000, 
        CancellationToken ct = default)
    {
        var queue = _streams.GetOrAdd(streamKey, _ => new ConcurrentQueue<string>());
        var json = JsonSerializer.Serialize(entries);
        queue.Enqueue(json);

        var id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-0";
        _logger.LogInformation("In-memory stream '{StreamKey}' appended raw entry {Id}", streamKey, id);
        return Task.FromResult(id);
    }
}
