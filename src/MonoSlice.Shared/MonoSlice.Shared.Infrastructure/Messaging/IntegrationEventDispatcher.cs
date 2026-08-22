using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Shared.Infrastructure.Messaging;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(string eventTypeName, string payload, CancellationToken cancellationToken = default);
    void RegisterEvent<TEvent>() where TEvent : IntegrationEvent;
    IReadOnlyCollection<Type> GetRegisteredEventTypes();
}

public sealed class IntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;
    private readonly Dictionary<string, Type> _eventTypeMap = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IntegrationEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventDispatcher> _logger)
    {
        _serviceProvider = serviceProvider;
        this._logger = _logger;
    }

    public void RegisterEvent<TEvent>() where TEvent : IntegrationEvent
    {
        var type = typeof(TEvent);
        _eventTypeMap[type.Name] = type;
    }

    public IReadOnlyCollection<Type> GetRegisteredEventTypes() => _eventTypeMap.Values;

    public async Task DispatchAsync(string eventTypeName, string payload, CancellationToken cancellationToken = default)
    {
        if (!_eventTypeMap.TryGetValue(eventTypeName, out var eventType))
        {
            _logger.LogDebug("No registered event model found for event type '{EventType}'", eventTypeName);
            return;
        }

        object? deserializedEvent;
        try
        {
            deserializedEvent = JsonSerializer.Deserialize(payload, eventType, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize integration event of type {EventType}", eventTypeName);
            return;
        }

        if (deserializedEvent is not IntegrationEvent integrationEvent)
        {
            _logger.LogWarning("Deserialized object is not an IntegrationEvent: {EventType}", eventTypeName);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            try
            {
                var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));
                if (method is not null)
                {
                    var task = (Task?)method.Invoke(handler, [integrationEvent, cancellationToken]);
                    if (task is not null)
                    {
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred handling integration event {EventType} with handler {HandlerType}",
                    eventTypeName, handler.GetType().Name);
                throw;
            }
        }
    }
}
