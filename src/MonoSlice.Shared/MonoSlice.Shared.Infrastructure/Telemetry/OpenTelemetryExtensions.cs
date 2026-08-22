using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MonoSlice.Shared.Infrastructure.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMonoSliceOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName = "MonoSlice")
    {
        var otelEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
        var otelServiceName = configuration["OpenTelemetry:ServiceName"] ?? serviceName;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: otelServiceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/scalar");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("MonoSlice.*");

                if (Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var uri))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = uri);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var uri))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = uri);
                }
            });

        return services;
    }

    public static ILoggingBuilder AddMonoSliceOtelLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string serviceName = "MonoSlice")
    {
        var otelEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
        var otelServiceName = configuration["OpenTelemetry:ServiceName"] ?? serviceName;

        if (Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var uri))
        {
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(otelServiceName));
                options.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = uri);
            });
        }

        return logging;
    }
}
