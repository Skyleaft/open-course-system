using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Storage;
using MonoSlice.Shared.Infrastructure.Behaviors;
using MonoSlice.Shared.Infrastructure.Caching;
using MonoSlice.Shared.Infrastructure.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging.Kafka;
using MonoSlice.Shared.Infrastructure.Messaging.RabbitMQ;
using MonoSlice.Shared.Infrastructure.Middleware;
using MonoSlice.Shared.Infrastructure.Persistence;
using MonoSlice.Shared.Infrastructure.Storage;
using StackExchange.Redis;

namespace MonoSlice.Shared.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCaching(configuration);
        services.AddStorage(configuration);
        services.AddMessaging(configuration);
        services.AddRealtimeHubs(configuration);
        services.AddMediatorBehaviors();
        services.AddDapper(configuration);

        return services;
    }

    public static IServiceCollection AddRealtimeHubs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var signalR = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        var redisConnStr = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnStr) && !redisConnStr.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                signalR.AddStackExchangeRedis(redisConnStr);
            }
            catch
            {
                // Fallback to local memory backplane
            }
        }

        return services;
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var minioSection = configuration.GetSection(StorageSettings.SectionName);
        services.Configure<StorageSettings>(minioSection);
        services.AddSingleton<IObjectStorageService, MinioObjectStorageService>();

        return services;
    }

    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheSection = configuration.GetSection(CacheSettings.SectionName);
        services.Configure<CacheSettings>(cacheSection);
        var cacheSettings = cacheSection.Get<CacheSettings>() ?? new CacheSettings();
        var redisConnStr = configuration.GetConnectionString("Redis") ?? cacheSettings.Redis?.ConnectionString;

        if (string.Equals(cacheSettings.Provider, "Redis", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(redisConnStr))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnStr;
            });
            services.AddSingleton<ICacheService, RedisCacheService>();

            try
            {
                var multiplexer = ConnectionMultiplexer.Connect(redisConnStr);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                services.AddSingleton<IDistributedLock, RedisDistributedLock>();
                services.AddSingleton<IEventStreamPublisher, RedisEventStreamPublisher>();
            }
            catch
            {
                // Fallback to in-memory for testing environments without live Redis
                services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
                services.AddSingleton<IEventStreamPublisher, InMemoryEventStreamPublisher>();
            }
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
            services.AddSingleton<IEventStreamPublisher, InMemoryEventStreamPublisher>();
        }

        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var messagingSection = configuration.GetSection(MessagingSettings.SectionName);
        services.Configure<MessagingSettings>(messagingSection);
        var messagingSettings = messagingSection.Get<MessagingSettings>() ?? new MessagingSettings();

        services.AddSingleton<IIntegrationEventDispatcher, IntegrationEventDispatcher>();

        if (string.Equals(messagingSettings.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEventBus, Messaging.InMemory.InMemoryEventBus>();
        }
        else if (string.Equals(messagingSettings.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEventBus, KafkaEventBus>();
            services.AddHostedService<KafkaConsumerHostedService>();
        }
        else
        {
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddHostedService<RabbitMqConsumerHostedService>();
        }

        return services;
    }

    public static IServiceCollection AddMediatorBehaviors(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricAndTraceBehavior<,>));
        return services;
    }

    public static IApplicationBuilder UseSharedMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
}
