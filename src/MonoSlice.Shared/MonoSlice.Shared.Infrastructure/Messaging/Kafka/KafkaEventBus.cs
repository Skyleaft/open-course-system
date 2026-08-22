using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Shared.Infrastructure.Messaging.Kafka;

public sealed class KafkaEventBus : IEventBus, IDisposable
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly IProducer<string, string> _producer;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaEventBus(IOptions<MessagingSettings> settings, ILogger<KafkaEventBus> logger)
    {
        _settings = settings.Value.Kafka;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        var eventName = integrationEvent.EventType;
        var topic = $"{_settings.TopicPrefix}{eventName.ToLowerInvariant()}";
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);

        var message = new Message<string, string>
        {
            Key = integrationEvent.Id.ToString(),
            Value = json,
            Headers =
            [
                new Header("EventType", System.Text.Encoding.UTF8.GetBytes(eventName))
            ]
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation("Published integration event {EventType} (ID: {EventId}) to Kafka topic {Topic} at offset {Offset}",
                eventName, integrationEvent.Id, topic, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver event {EventType} to Kafka topic {Topic}: {Reason}",
                eventName, topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
