using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonoSlice.Shared.Infrastructure.Messaging.Kafka;

public sealed class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly ILogger<KafkaConsumerHostedService> _logger;

    public KafkaConsumerHostedService(
        IOptions<MessagingSettings> settings,
        IIntegrationEventDispatcher dispatcher,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _settings = settings.Value.Kafka;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
    }

    private void StartConsumer(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_settings.AutoOffsetReset, true, out var offset)
                ? offset
                : Confluent.Kafka.AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        var registeredTypes = _dispatcher.GetRegisteredEventTypes();
        var topics = registeredTypes
            .Select(t => $"{_settings.TopicPrefix}{t.Name.ToLowerInvariant()}")
            .ToList();

        if (topics.Count == 0)
        {
            topics.Add($"{_settings.TopicPrefix}*");
        }

        try
        {
            consumer.Subscribe(topics);
            _logger.LogInformation("Kafka consumer subscribed to topics: {Topics}", string.Join(", ", topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult?.Message is null) continue;

                    string? eventType = null;
                    if (consumeResult.Message.Headers.TryGetLastBytes("EventType", out var headerBytes))
                    {
                        eventType = Encoding.UTF8.GetString(headerBytes);
                    }
                    else
                    {
                        var topicWithoutPrefix = consumeResult.Topic.StartsWith(_settings.TopicPrefix, StringComparison.OrdinalIgnoreCase)
                            ? consumeResult.Topic[_settings.TopicPrefix.Length..]
                            : consumeResult.Topic;
                        eventType = topicWithoutPrefix;
                    }

                    _logger.LogInformation("Received Kafka message on topic '{Topic}' with event type '{EventType}'",
                        consumeResult.Topic, eventType);

                    _dispatcher.DispatchAsync(eventType, consumeResult.Message.Value, stoppingToken)
                        .GetAwaiter()
                        .GetResult();

                    consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming from Kafka: {Reason}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Kafka consumer stopping gracefully");
        }
        finally
        {
            consumer.Close();
        }
    }
}
