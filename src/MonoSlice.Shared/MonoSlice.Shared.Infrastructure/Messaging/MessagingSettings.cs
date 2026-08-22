namespace MonoSlice.Shared.Infrastructure.Messaging;

public sealed class MessagingSettings
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// Message broker provider: "RabbitMQ" or "Kafka"
    /// </summary>
    public string Provider { get; set; } = "RabbitMQ";

    public RabbitMqSettings RabbitMQ { get; set; } = new();
    public KafkaSettings Kafka { get; set; } = new();
}

public sealed class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "monoslice.events";
    public string QueueName { get; set; } = "monoslice.queue";
}

public sealed class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "monoslice-group";
    public string TopicPrefix { get; set; } = "monoslice-";
    public string AutoOffsetReset { get; set; } = "Earliest";
}
