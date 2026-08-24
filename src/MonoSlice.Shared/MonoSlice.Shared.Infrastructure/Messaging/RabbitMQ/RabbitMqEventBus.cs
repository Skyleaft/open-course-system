using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Messaging;
using RabbitMQ.Client;

namespace MonoSlice.Shared.Infrastructure.Messaging.RabbitMQ;

public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqEventBus(IOptions<MessagingSettings> settings, ILogger<RabbitMqEventBus> logger)
    {
        _settings = settings.Value.RabbitMQ;
        _logger = logger;
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _channel.IsOpen) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is not null && _channel.IsOpen) return;

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} with exchange '{Exchange}'",
                _settings.Host, _settings.Port, _settings.ExchangeName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        await EnsureChannelAsync(cancellationToken);

        var eventName = integrationEvent.EventType;
        var json = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = eventName,
            MessageId = integrationEvent.Id.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var routingKey = eventName.ToLowerInvariant();

        if (_channel is not null)
        {
            await _channel.BasicPublishAsync(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Published integration event {EventType} (ID: {EventId}) to RabbitMQ",
                eventName, integrationEvent.Id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _lock.Dispose();
    }
}
