using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Confluent.Kafka;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Localization.Client;
using Nudges.Telemetry;
using Nudges.Webhooks;
using Nudges.Webhooks.Endpoints;
using Nudges.Webhooks.Endpoints.Handlers;
using Nudges.Webhooks.Stripe;
using Nudges.Webhooks.Stripe.Commands;
using Nudges.Webhooks.Twilio;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Precision.WarpCache;
using Precision.WarpCache.Grpc.Client;
using Precision.WarpCache.MemoryCache;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// set configuration sources
builder.Configuration
    .AddUserSecrets(typeof(Program).Assembly)
    .AddEnvironmentVariables();

// configure Oidc settings
builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection("Oidc"));

// configure logging
builder.Logging.AddSimpleConsole(o => {
    o.SingleLine = true;
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
});


// configure OpenTelemetry
if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {
    builder.Services.AddOpenTelemetryConfiguration(
        url,
        builder.Environment.ApplicationName, [
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http",
            "Nudges.Webhooks",
            $"{typeof(StripeWebhookHandler).FullName}",
            $"{typeof(TwilioWebhookHandler).FullName}",
        ], [
            "Nudges.Webhooks",
            $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
            $"{typeof(StripeWebhookHandler).FullName}",
            $"{typeof(TwilioWebhookHandler).FullName}",
        ], null, null, o => o.Filter = ctx => ctx.Request.Method == "POST");
}

// configure Kafka producers
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

// configure Stripe client
builder.Services.AddSingleton<IStripeClient>(s =>
    new StripeClient(builder.Configuration.GetStripeApiKey(), apiBase: builder.Configuration.GetStripeApiUrl()));
builder.Services.AddTransient<IStripeVerifier, StripeVerifier>();


// configure GraphQL client
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<INudgesClient>(NudgesClient.ClientName)
    .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// configure stripe webhook handlers
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
builder.Services.AddTransient<StripeWebhookHandler>();

// configure twilio webhook handlers
builder.Services.AddSingleton<IMessageSender, MessageSender>();
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
builder.Services.AddTransient<TwilioWebhookHandler>();

builder.Services.AddLocalizationClient(builder.Configuration.GetLocalizationApiUrl());

// configure WarpCache
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICacheStore<string, string>, MemoryCacheStore<string, string>>();
builder.Services.AddSingleton<IEvictionPolicy<string>>(new LruEvictionPolicy<string>(1000));
builder.Services.AddSingleton<ChannelCacheMediator<string, string>>();
builder.Services.AddWarpCacheClient(
    builder.Configuration.GetCacheServerAddress(),
    StringMessageSerializerContext.Default.String);

// configure server token client
builder.Services.AddHttpClient<IServerTokenClient, ServerTokenClient>()
    .ConfigurePrimaryHttpMessageHandler(sp => {
        var env = sp.GetRequiredService<IHostEnvironment>();
        return env.IsDevelopment()
            ? new HttpClientHandler {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            } : new HttpClientHandler();
    })
    .ConfigureHttpClient(client =>
        client.BaseAddress = new Uri(builder.Configuration.GetOidcServerUrl()));

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

builder.Services.AddHeaderPropagation(o => {
    o.Headers.Add("traceparent");
    o.Headers.Add("tracestate");
    o.Headers.Add("baggage");
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.Use(async (context, next) => {
    // Skip non-webhook paths
    if (!context.Request.Path.StartsWithSegments("/api/StripeWebhookHandler") &&
        !context.Request.Path.StartsWithSegments("/api/TwilioWebhookHandler")) {
        await next.Invoke(context);
        return;
    }

    // Enable buffering so we can read the body multiple times
    context.Request.EnableBuffering();

    // Try to extract trace context based on the endpoint
    ActivityContext parentContext = default;

    if (context.Request.Path.StartsWithSegments("/api/StripeWebhookHandler")) {
        // Read the body to extract idempotency key
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Reset for next reader

        try {
            var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("request", out var request) &&
                request.TryGetProperty("idempotency_key", out var key)) {
                var idempotencyKey = key.GetString();
                if (idempotencyKey != null &&
                    ActivityContext.TryParse(idempotencyKey, default, out var ctx)) {
                    parentContext = ctx;
                }
            }
        } catch {
            // If parsing fails, just continue without correlation
        }
    } else if (context.Request.Path.StartsWithSegments("/api/TwilioWebhookHandler")) {
        // Similar logic for Twilio if needed
        // Twilio might send trace context in headers or form fields
    }

    // If we found a parent context, create an activity for this request
    if (parentContext != default) {
        var activity = ActivitySource.CreateActivity(
            $"WebhookRequest {context.Request.Path}",
            ActivityKind.Server,
            parentContext);
        activity?.Start();

        try {
            await next.Invoke(context);
        } finally {
            activity?.Stop();
        }
    } else {
        await next.Invoke(context);
    }
});

app.Use(async (context, next) => {
    if (!context.Request.Path.StartsWithSegments("/api")) {
        await next.Invoke(context);
        return;
    }

    if (context.Request.Query.TryGetValue("code", out var value) &&
        value == context.RequestServices.GetRequiredService<IConfiguration>().GetApiKey()) {

        await next.Invoke(context);
        return;
    }

    context.Request.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
        .LogUnauthorized(context.Request.Path);
    Activity.Current?.SetStatus(ActivityStatusCode.Error, "Unauthorized request");
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Unauthorized");
});

var api = app.MapGroup("/api");

api.MapPost("/StripeWebhookHandler", async (StripeWebhookHandler handler, HttpContext context) =>
    // TODO: look into better retry logic since I'm using Result<,> now.
    await RetryPolicy.ExecuteAsync(() => handler.Endpoint(context.Request, context.RequestAborted)));

api.MapPost("/TwilioWebhookHandler", async (TwilioWebhookHandler handler, HttpContext context) =>
    // TODO: look into better retry logic since I'm using Result<,> now.
    RetryPolicy.ExecuteAsync(() => handler.Endpoint(context.Request, context.RequestAborted)));

app.Run();

internal partial class Program {
    private static readonly ActivitySource ActivitySource = new(
        "Nudges.Webhooks",
        "1.0.0"
    );


    private static readonly AsyncPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(Backoff.ExponentialBackoff(TimeSpan.FromSeconds(1), retryCount: 5));
}
