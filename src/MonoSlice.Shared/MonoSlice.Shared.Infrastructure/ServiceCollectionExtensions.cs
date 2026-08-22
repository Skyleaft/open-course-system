using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Infrastructure.Behaviors;
using MonoSlice.Shared.Infrastructure.Caching;
using MonoSlice.Shared.Infrastructure.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging.Kafka;
using MonoSlice.Shared.Infrastructure.Messaging.RabbitMQ;
using MonoSlice.Shared.Infrastructure.Middleware;
using MonoSlice.Shared.Infrastructure.Persistence;

namespace MonoSlice.Shared.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCaching(configuration);
        services.AddMessaging(configuration);
        services.AddMediatorBehaviors();
        services.AddDapper(configuration);

        return services;
    }

    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheSection = configuration.GetSection(CacheSettings.SectionName);
        services.Configure<CacheSettings>(cacheSection);
        var cacheSettings = cacheSection.Get<CacheSettings>() ?? new CacheSettings();

        if (string.Equals(cacheSettings.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.Redis.ConnectionString;
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
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

        if (string.Equals(messagingSettings.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
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
        return services;
    }

    public static IApplicationBuilder UseSharedMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
}
