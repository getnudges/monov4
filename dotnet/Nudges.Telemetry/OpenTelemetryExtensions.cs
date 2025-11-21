using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nudges.Telemetry;

public static class OpenTelemetryExtensions {
    public static IServiceCollection AddOpenTelemetryConfiguration(
        this IServiceCollection services,
        string otelUrl,
        string serviceName,
        string[]? meters = null,
        string[]? sources = null,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null) {

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName))
            .WithMetrics(metricsConfig => {
                metricsConfig
                    .AddRuntimeInstrumentation()
                    .AddMeter(meters ?? [])
                    .AddPrometheusExporter();

                configureMetrics?.Invoke(metricsConfig);
            })
            .WithTracing(traceConfig => {
                traceConfig
                    .SetSampler<AlwaysOnSampler>()
                    .AddAspNetCoreInstrumentation(o => {
                        o.RecordException = true;
                        o.Filter = context =>
                            context.Request.Method != "GET";
                    })
                    .AddHttpClientInstrumentation(o =>
                        o.RecordException = true)
                    .AddSource(sources ?? []);

                // Allow additional configuration
                configureTracing?.Invoke(traceConfig);
            })
            .WithLogging();

        // Configure OTLP exporters
        var otlpEndpoint = new Uri(otelUrl);
        services.ConfigureOpenTelemetryTracerProvider(o =>
            o.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint));
        services.ConfigureOpenTelemetryLoggerProvider(o =>
            o.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint));
        services.ConfigureOpenTelemetryMeterProvider(o =>
            o.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = otlpEndpoint));

        return services;
    }
}
