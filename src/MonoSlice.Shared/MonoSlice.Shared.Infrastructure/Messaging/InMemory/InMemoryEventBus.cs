using System.Text.Json;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Shared.Infrastructure.Messaging.InMemory;

/// <summary>
/// In-memory event bus implementation that directly dispatches integration events to registered handlers via IIntegrationEventDispatcher.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<InMemoryEventBus> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InMemoryEventBus(
        IIntegrationEventDispatcher dispatcher,
        ILogger<InMemoryEventBus> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        var eventName = integrationEvent.EventType;
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);

        _logger.LogInformation("Dispatching in-memory integration event {EventType} (ID: {EventId})",
            eventName, integrationEvent.Id);

        await _dispatcher.DispatchAsync(eventName, json, cancellationToken);
    }
}
