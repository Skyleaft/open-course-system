using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Orders.EventHandlers;
using MonoSlice.Modules.Orders.Features.CancelOrder;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Modules.Orders.Features.GetOrder;
using MonoSlice.Modules.Orders.Features.GetOrderAnalytics;
using MonoSlice.Modules.Orders.Features.ListOrders;
using MonoSlice.Modules.Orders.Features.ProcessOrderAsync;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Modules.Orders.Services;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDb") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=monoslice_orders;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.DefaultSchema);
                });
            });
        }

        // Register in-process asynchronous queue and background worker
        services.AddSingleton<IOrderProcessingQueue, OrderProcessingChannelQueue>();
        services.AddHostedService<OrderProcessingBackgroundService>();

        // Register integration event handlers
        services.AddTransient<IIntegrationEventHandler<OrderCompletedIntegrationEvent>, OrderCompletedIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        // Register event types with dispatcher so message consumers know how to deserialize them
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<OrderPlacedIntegrationEvent>();
        dispatcher?.RegisterEvent<OrderCompletedIntegrationEvent>();
        dispatcher?.RegisterEvent<OrderCancelledIntegrationEvent>();

        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapCreateOrderEndpoint();
        group.MapGetOrderEndpoint();
        group.MapListOrdersEndpoint();
        group.MapProcessOrderAsyncEndpoint();
        group.MapCancelOrderEndpoint();
        group.MapGetOrderAnalyticsEndpoint();

        return app;
    }
}
