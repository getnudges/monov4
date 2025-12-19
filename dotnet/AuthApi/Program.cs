using System.Net.Http.Headers;
using System.Net.Mime;
using AuthApi;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Nudges.Auth;
using Nudges.Auth.Keycloak;
using Nudges.Auth.Web;
using Nudges.Configuration;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Security;
using Nudges.Telemetry;
using Precision.WarpCache.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

builder.Services.Configure<OidcSettings>(builder.Configuration.GetSection(nameof(Settings.Oidc)));
builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection(nameof(Settings.Oidc)));

if (settings.Otlp.Endpoint is string url) {

    builder.Services.AddOpenTelemetryConfiguration<Program>(
            url,
            builder.Environment.ApplicationName, [
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "System.Net.Http",
            ], [
                $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
                $"{typeof(ApiController).FullName}"
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

builder.Services.AddSingleton(static sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var base64 = config["HashSettings:HashKeyBase64"]
        ?? throw new InvalidOperationException("Missing HashSettings:HashKeyBase64");

    var keyBytes = Convert.FromBase64String(base64);
    return new HashService(
        Options.Create(new HashSettings { HashKey = keyBytes })
    );
});
builder.Services.AddSingleton<IEncryptionService>(static sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var base64 = config["EncryptionSettings:Key"]
        ?? throw new InvalidOperationException("Missing EncryptionSettings:Key");

    var keyBytes = Convert.FromBase64String(base64);
    return new AesGcmEncryptionService(
        Options.Create(new EncryptionSettings { Key = keyBytes })
    );
});

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

if (settings.Otlp.Endpoint is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.MapControllers();

await app.RunAsync();
