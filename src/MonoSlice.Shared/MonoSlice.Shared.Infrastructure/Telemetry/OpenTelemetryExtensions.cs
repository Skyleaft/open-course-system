using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
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
        var (endpoint, resolvedServiceName, protocol, headers) = GetOtelSettings(configuration, serviceName);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: resolvedServiceName, serviceVersion: "1.0.0"))
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
                    .AddSource("MonoSlice.*")
                    .AddSource("MonoSlice.Mediator")
                    .AddSource("MonoSlice.EventStream")
                    .AddSource("MonoSlice.Assessments.Worker");

                if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = uri;
                        options.Protocol = protocol;
                        if (!string.IsNullOrWhiteSpace(headers))
                        {
                            options.Headers = headers;
                        }
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("MonoSlice.*")
                    .AddMeter("MonoSlice.Mediator")
                    .AddMeter("MonoSlice.EventStream")
                    .AddMeter("MonoSlice.Assessments.Worker");

                if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = uri;
                        options.Protocol = protocol;
                        if (!string.IsNullOrWhiteSpace(headers))
                        {
                            options.Headers = headers;
                        }
                    });
                }
            });

        return services;
    }

    public static ILoggingBuilder AddMonoSliceOtelLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string serviceName = "MonoSlice")
    {
        var (endpoint, resolvedServiceName, protocol, headers) = GetOtelSettings(configuration, serviceName);

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName: resolvedServiceName, serviceVersion: "1.0.0"));
                options.AddOtlpExporter(exporterOptions =>
                {
                    exporterOptions.Endpoint = uri;
                    exporterOptions.Protocol = protocol;
                    if (!string.IsNullOrWhiteSpace(headers))
                    {
                        exporterOptions.Headers = headers;
                    }
                });
            });
        }

        return logging;
    }

    private static (string Endpoint, string ServiceName, OtlpExportProtocol Protocol, string? Headers) GetOtelSettings(
        IConfiguration configuration,
        string defaultServiceName)
    {
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? configuration["OpenTelemetry:Endpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? "http://localhost:4317";

        var serviceName = configuration["OTEL_SERVICE_NAME"]
            ?? configuration["OpenTelemetry:ServiceName"]
            ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? defaultServiceName;

        var protocolStr = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]
            ?? configuration["OpenTelemetry:Protocol"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");

        var headers = configuration["OTEL_EXPORTER_OTLP_HEADERS"]
            ?? configuration["OpenTelemetry:Headers"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");

        var protocol = ResolveProtocol(protocolStr);

        return (endpoint, serviceName, protocol, headers);
    }

    private static OtlpExportProtocol ResolveProtocol(string? protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return OtlpExportProtocol.Grpc;
        }

        var normalized = protocol.Trim().ToLowerInvariant();
        return normalized switch
        {
            "http/protobuf" or "httpprotobuf" or "http" => OtlpExportProtocol.HttpProtobuf,
            "grpc" => OtlpExportProtocol.Grpc,
            _ => OtlpExportProtocol.Grpc
        };
    }
}
