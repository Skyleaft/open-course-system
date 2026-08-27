using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Orders.Contracts;
using MonoSlice.Modules.Orders.Features.CreateCheckout;
using MonoSlice.Modules.Orders.Features.GetOrder;
using MonoSlice.Modules.Orders.Features.GetRevenueAnalytics;
using MonoSlice.Modules.Orders.Features.ProcessWebhook;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var paymentsSection = configuration.GetSection(PaymentsSettings.SectionName);
        services.Configure<PaymentsSettings>(paymentsSection);

        var connectionString = configuration.GetConnectionString("PaymentsDb") ??
                               configuration.GetConnectionString("OrdersDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PaymentsDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<PaymentsDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PaymentsDbContext.DefaultSchema);
                });
            });
        }

        // Register Payments Inter-Module API
        services.AddScoped<IPaymentsModuleApi, PaymentsModuleApi>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var paymentsV1Group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments");

        paymentsV1Group.MapCreateCheckoutEndpoint();
        paymentsV1Group.MapProcessWebhookEndpoint();
        paymentsV1Group.MapGetOrderEndpoint();

        var dashboardGroup = app.MapGroup("/api/v1")
            .WithTags("Dashboard");
        dashboardGroup.MapGetRevenueAnalyticsEndpoint();

        return app;
    }
}
