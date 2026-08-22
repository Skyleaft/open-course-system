using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Catalog.Contracts;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.EventHandlers;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Modules.Catalog.Features.DeleteProduct;
using MonoSlice.Modules.Catalog.Features.GetProduct;
using MonoSlice.Modules.Catalog.Features.ListProducts;
using MonoSlice.Modules.Catalog.Features.UpdateProduct;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CatalogDb") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=monoslice_catalog;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.DefaultSchema);
                });
            });
        }

        // Register module contract API for synchronous inter-module communication
        services.AddScoped<ICatalogModuleApi, CatalogModuleApi>();

        // Register integration event handlers for asynchronous inter-module messaging
        services.AddTransient<IIntegrationEventHandler<ProductCreatedIntegrationEvent>, ProductCreatedIntegrationEventHandler>();
        services.AddTransient<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, OrderPlacedIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        // Register event types with dispatcher so background consumers know how to deserialize them
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<ProductCreatedIntegrationEvent>();
        dispatcher?.RegisterEvent<OrderPlacedIntegrationEvent>();

        var group = app.MapGroup("/api/catalog/products")
            .WithTags("Catalog");

        group.MapCreateProductEndpoint();
        group.MapGetProductEndpoint();
        group.MapListProductsEndpoint();
        group.MapUpdateProductEndpoint();
        group.MapDeleteProductEndpoint();

        return app;
    }
}

