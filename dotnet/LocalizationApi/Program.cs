using System.Globalization;
using LocalizationApi.Services;
using Microsoft.AspNetCore.Localization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource
                .AddService(builder.Environment.ApplicationName))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                .AddAspNetCoreInstrumentation())
        .WithLogging();
        
    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Services.AddLocalization();

var app = builder.Build();

var supportedCultures = new[] {
    new CultureInfo("en-US"),
    new CultureInfo("es-ES")
};

app.UseRequestLocalization(new RequestLocalizationOptions {
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.MapGrpcService<LocalizationService>();

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.Run();
