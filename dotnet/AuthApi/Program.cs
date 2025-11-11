using System.Net.Http.Headers;
using System.Net.Mime;
using AuthApi;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Nudges.Auth;
using Nudges.Auth.Keycloak;
using Nudges.Auth.Web;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Precision.WarpCache.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddFlatFilesFromMap(
    builder.Configuration.GetValue("FILEMAP", string.Empty),
    !builder.Environment.IsDevelopment());

builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection("Oidc"));

if (builder.Configuration.GetOltpEndpointUrl() is string url) {

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(builder.Environment.ApplicationName))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http"
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context =>
                        context.Request.Method != "GET")
                .AddSource([
                    "Nudges.Kafka.KafkaMessageProducer",
                    "Nudges.Telemetry.TracingMiddleware"
                ]))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryMeterProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Services.AddExceptionHandler(o =>
    o.ExceptionHandler = async context => {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error is not null) {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogRequestException(error.Error.Message, error.Error);
        }
        await context.Response.WriteAsJsonAsync(new ErrorResponse(
            error?.Error.Message ?? "Unknown error",
            context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
            ? error?.Error.StackTrace
            : null), ErrorResponseSerializerContext.Default.ErrorResponse);
    });

builder.Services.AddWarpCacheClient(
    builder.Configuration.GetCacheServerAddress(),
    StringMessageSerializerContext.Default.String);

builder.Services.AddLogging(configure => configure
        .AddSimpleConsole(o =>
            o.SingleLine = !builder.Environment.IsDevelopment()));

builder.Services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));

builder.Services.AddTransient<IOtpVerifier, OtpVerifier>();

builder.Services.AddHttpClient<IKeycloakOidcClient, KeycloakOidcClient>(client => {
    client.BaseAddress = new Uri(builder.Configuration.GetOidcServerUrl());
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
}).ConfigurePrimaryHttpMessageHandler(s => {
    if (builder.Configuration.GetIgnoreSslCertValidation() == "true") {
        return new HttpClientHandler {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
    return new HttpClientHandler();
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (builder.Environment.IsDevelopment()) {
    app.UseForwardedHeaders(new ForwardedHeadersOptions {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    });
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");

if (builder.Configuration.GetOltpEndpointUrl() is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.MapGet("/otp", Handlers.GenerateOtp);
app.MapPost("/otp", Handlers.ValidateOtp);
app.MapGet("/login", Handlers.OAuthLogin);
app.MapGet("/redirect", Handlers.OAuthRedirect);

await app.RunAsync();
