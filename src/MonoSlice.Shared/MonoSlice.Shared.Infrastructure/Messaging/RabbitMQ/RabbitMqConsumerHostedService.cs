using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MonoSlice.Shared.Infrastructure.Messaging.RabbitMQ;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerHostedService(
        IOptions<MessagingSettings> settings,
        IIntegrationEventDispatcher dispatcher,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        _settings = settings.Value.RabbitMQ;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // Bind registered event types or '#' for all events
            await _channel.QueueBindAsync(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: "#",
                arguments: null,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventType = ea.BasicProperties.Type ?? ea.RoutingKey;

                _logger.LogInformation("Received RabbitMQ message with event type '{EventType}'", eventType);

                try
                {
                    await _dispatcher.DispatchAsync(eventType, message, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message {EventType}. Rejecting.", eventType);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ consumer listening on queue '{Queue}' bound to '{Exchange}'",
                _settings.QueueName, _settings.ExchangeName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("RabbitMQ consumer is stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ consumer encountered an unexpected error");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is { IsOpen: true })
            {
                await _channel.CloseAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Channel already closed or failed to close.");
        }
        finally
        {
            _channel?.Dispose();
            _channel = null;
        }

        try
        {
            if (_connection is { IsOpen: true })
            {
                await _connection.CloseAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Connection already closed or failed to close.");
        }
        finally
        {
            _connection?.Dispose();
            _connection = null;
        }

        await base.StopAsync(cancellationToken);
    }
}
