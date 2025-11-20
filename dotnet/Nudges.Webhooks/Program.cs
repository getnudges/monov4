using System.Net.Http.Headers;
using Confluent.Kafka;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Precision.WarpCache;
using Precision.WarpCache.Grpc.Client;
using Precision.WarpCache.MemoryCache;
using Stripe;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Localization.Client;
using Nudges.Webhooks;
using Nudges.Webhooks.Endpoints;
using Nudges.Webhooks.Endpoints.Handlers;
using Nudges.Webhooks.Stripe;
using Nudges.Webhooks.Stripe.Commands;
using Nudges.Webhooks.Twilio;
using Nudges.Kafka.Events;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .AddUserSecrets(typeof(Program).Assembly)
    .AddEnvironmentVariables()
    .AddFlatFilesFromMap(
        builder.Configuration.GetValue("FILEMAP", string.Empty),
        !builder.Environment.IsDevelopment());

builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection("Oidc"));

builder.Logging.AddSimpleConsole(o => o.SingleLine = true);

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(builder.Environment.ApplicationName))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    $"{typeof(StripeWebhookHandler).FullName}",
                    $"{typeof(TwilioWebhookHandler).FullName}",
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                .AddGrpcClientInstrumentation()
                .AddAspNetCoreInstrumentation(options => {
                    options.Filter = context => context.Request.Method == "POST";
                    options.RecordException = true;
                })
                .AddSource([
                    $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
                    $"{typeof(StripeWebhookHandler).FullName}",
                    $"{typeof(TwilioWebhookHandler).FullName}",
                ]))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Services.AddSingleton<KafkaMessageProducer<PaymentKey, PaymentEvent>>(static sp =>
    new PaymentEventProducer(Topics.Payments, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));
builder.Services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));
builder.Services.AddSingleton<KafkaMessageProducer<ForeignProductEventKey, ForeignProductEvent>>(static sp =>
    new ForeignProductEventProducer(Topics.ForeignProducts, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));

builder.Services.AddSingleton<IStripeClient>(s =>
    new StripeClient(builder.Configuration.GetStripeApiKey(), apiBase: builder.Configuration.GetStripeApiUrl()));

builder.Services.AddLocalizationClient(builder.Configuration.GetLocalizationApiUrl());

builder.Services.AddTransient<IStripeVerifier, StripeVerifier>();

builder.Services.AddSingleton<IMessageSender, MessageSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<INudgesClient>(NudgesClient.ClientName)
    .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddTransient<ProductCreatedCommand>();
builder.Services.AddTransient<PriceDeletedCommand>();
builder.Services.AddTransient<ProductDeletedCommand>();
builder.Services.AddTransient<PriceUpdatedCommand>();
builder.Services.AddTransient<PriceCreatedCommand>();
builder.Services.AddTransient<ProductUpdatedCommand>();
builder.Services.AddTransient<CheckoutSessionCompletedCommand>();

builder.Services.AddSingleton(s => new StripeEventCommandProcessorBuilder()
    // https://docs.stripe.com/api/events/types
    .AddHandler("price.deleted", s.GetRequiredService<PriceDeletedCommand>())
    .AddHandler("product.created", s.GetRequiredService<ProductCreatedCommand>())
    .AddHandler("product.deleted", s.GetRequiredService<ProductDeletedCommand>())
    .AddHandler("product.updated", s.GetRequiredService<ProductUpdatedCommand>())
    .AddHandler("price.updated", s.GetRequiredService<PriceUpdatedCommand>())
    .AddHandler("price.created", s.GetRequiredService<PriceCreatedCommand>())
    .AddHandler("checkout.session.completed", s.GetRequiredService<CheckoutSessionCompletedCommand>())
    .Build());

builder.Services.AddTransient<CommandsCommand>();
builder.Services.AddTransient<UnsubCommand>();
builder.Services.AddTransient<AnnouncementCommand>();
builder.Services.AddTransient<AnnouncementConfirmCommand>();
builder.Services.AddSingleton(s => new TwilioEventCommandProcessorBuilder()
    .AddHandler(CommandsCommand.Regex, s.GetRequiredService<CommandsCommand>())
    .AddHandler(CommandsCommand.HelpRegex, s.GetRequiredService<CommandsCommand>())
    .AddHandler(UnsubCommand.Regex, s.GetRequiredService<UnsubCommand>())
    .AddHandler(AnnouncementConfirmCommand.Regex, s.GetRequiredService<AnnouncementConfirmCommand>())
    // this command should *always* be last
    .AddHandler(AnnouncementCommand.Regex, s.GetRequiredService<AnnouncementCommand>())
    .Build());

builder.Services.AddTransient<StripeWebhookHandler>();
builder.Services.AddTransient<TwilioWebhookHandler>();

builder.Services.AddWarpCacheClient(
    builder.Configuration.GetCacheServerAddress(),
    StringMessageSerializerContext.Default.String);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICacheStore<string, string>, MemoryCacheStore<string, string>>();
builder.Services.AddSingleton<IEvictionPolicy<string>>(new LruEvictionPolicy<string>(1000));
builder.Services.AddSingleton<ChannelCacheMediator<string, string>>();

builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection("Oidc"));

builder.Services.AddHttpClient<IServerTokenClient, ServerTokenClient>()
    .ConfigurePrimaryHttpMessageHandler(sp => {
        var env = sp.GetRequiredService<IHostEnvironment>();
        return env.IsDevelopment()
            ? new HttpClientHandler {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            } : new HttpClientHandler();
    })
    .ConfigureHttpClient(client => {
        client.BaseAddress = new Uri(builder.Configuration.GetOidcServerUrl());
    });
builder.Services.AddNudgesClient()
    .ConfigureHttpClient((sp, client) => {
        var config = sp.GetRequiredService<IConfiguration>();
        client.BaseAddress = new Uri(config.GetGraphQLApiUrl());
        using var scope = sp.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<IServerTokenClient>()
            .GetTokenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        token.Match(token => {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            // TODO: this throw is intentional.  It should break the startup.
        }, e => throw e);
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/health")) {
        await next.Invoke(context);
        return;
    }
    if (context.Request.Path.StartsWithSegments("/metrics")) {
        await next.Invoke(context);
        return;
    }
    if (context.Request.Query.TryGetValue("code", out var value) &&
        value == context.RequestServices.GetRequiredService<IConfiguration>().GetApiKey()) {

        await next.Invoke(context);
        return;
    }
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Unauthorized");
});

var api = app.MapGroup("/api");

api.MapPost("/StripeWebhookHandler", async (StripeWebhookHandler handler, HttpContext context) =>
    // TODO: look into better retry logic since I'm using Result<,> now.
    await RetryPolicy.ExecuteAsync(() => handler.Endpoint(context.Request)));

api.MapPost("/TwilioWebhookHandler", async (TwilioWebhookHandler handler, HttpContext context, CancellationToken cancellationToken) =>
    // TODO: look into better retry logic since I'm using Result<,> now.
    await handler.Endpoint(context.Request, cancellationToken));

app.Run();

internal partial class Program {

    private static readonly AsyncPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(Backoff.ExponentialBackoff(TimeSpan.FromSeconds(1), retryCount: 5));
}
