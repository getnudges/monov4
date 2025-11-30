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
using Nudges.Kafka.Events;
using Nudges.Telemetry;
using Precision.WarpCache.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

if (settings.Otlp.Endpoint is string url)
{

    builder.Services.AddOpenTelemetryConfiguration<Program>(
            url,
            builder.Environment.ApplicationName, [
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "System.Net.Http",
            ], [
                $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
                $"{typeof(Handlers).FullName}"
            ], null, null, options => options.RecordException = true);
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
    settings.WarpCache,
    StringMessageSerializerContext.Default.String);

builder.Services.AddLogging(configure => configure
        .AddSimpleConsole(o =>
            o.SingleLine = !builder.Environment.IsDevelopment()));

builder.Services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
        BootstrapServers = settings.Kafka.BrokerList,
    }));
builder.Services.AddSingleton<KafkaMessageProducer<UserAuthenticationEventKey, UserAuthenticationEvent>>(sp =>
    new UserAuthenticationEventProducer(Topics.UserAuthentication, new ProducerConfig {
        BootstrapServers = settings.Kafka.BrokerList,
    }));

builder.Services.AddTransient<IOtpVerifier, OtpVerifier>();

builder.Services.AddHttpClient<IOidcClient, KeycloakOidcClient>(client => {
    client.BaseAddress = new Uri(settings.Oidc.ServerUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
}).ConfigurePrimaryHttpMessageHandler(s => {
    if (builder.Configuration.GetIgnoreSslCertValidation() == "true") {
        return new HttpClientHandler {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
    return new HttpClientHandler();
});

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (builder.Environment.IsDevelopment()) {
    app.UseForwardedHeaders(new ForwardedHeadersOptions {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    });
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");

if (settings.Otlp.Endpoint is not null)
{
    app.MapPrometheusScrapingEndpoint();
}

app.MapControllers();

await app.RunAsync();
