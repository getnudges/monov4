using System.CommandLine;
using System.Net.Http.Headers;
using Confluent.Kafka;
using KafkaConsumer;
using KafkaConsumer.Middleware;
using KafkaConsumer.Notifications;
using KafkaConsumer.Services;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Nudges.Localization.Client;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Precision.WarpCache;
using Precision.WarpCache.Grpc.Client;
using Precision.WarpCache.MemoryCache;
using Stripe;
using Twilio;
using Twilio.Clients;

var notificationsCmd = new Command("notifications");
var plansCmd = new Command("plans");
var priceTierCmd = new Command("price-tiers");
var planSubscriptionsCmd = new Command("plan-subscriptions");
var paymentsCmd = new Command("payments");
var clientsCmd = new Command("clients");
var userAuthCmd = new Command("user-authentication");

notificationsCmd.SetAction(r => CreateBaseHost(args, "notifications").ConfigureNotificationHandler().Build().RunAsync());
plansCmd.SetAction(r => CreateBaseHost(args, "plans").ConfigurePlanEventHandler().Build().RunAsync());
priceTierCmd.SetAction(r => CreateBaseHost(args, "price-tiers").ConfigurePriceTierEventHandler().Build().RunAsync());
paymentsCmd.SetAction(r => CreateBaseHost(args, "payments").ConfigurePaymentEventHandler().Build().RunAsync());
planSubscriptionsCmd.SetAction(r => CreateBaseHost(args, "plan-subscriptions").ConfigurePlanSubscriptionHandler().Build().RunAsync());
clientsCmd.SetAction(r => CreateBaseHost(args, "clients").ConfigureClientHandler().Build().RunAsync());
userAuthCmd.SetAction(r => CreateBaseHost(args, "user-authentication").ConfigureUserAuthenticationHandler().Build().RunAsync());

var rootCommand = new RootCommand {
    notificationsCmd,
    plansCmd,
    priceTierCmd,
    paymentsCmd,
    planSubscriptionsCmd,
    clientsCmd,
    userAuthCmd
};

await rootCommand.Parse(args).InvokeAsync();

static IHostBuilder CreateBaseHost(string[] args, string name) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(configure => {
            configure.ConfigureKestrel(options => { }).PreferHostingUrls(true);
            configure.Configure(configure => {
                if (configure.ApplicationServices.GetRequiredService<IConfiguration>().GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
                    configure.UseRouting().UseEndpoints(e => e.MapPrometheusScrapingEndpoint());
                }
            });
        })
        .ConfigureAppConfiguration(static (hostingContext, config) =>
            config
                .AddFlatFilesFromMap(
                    hostingContext.Configuration.GetValue("FILEMAP", string.Empty),
                    !hostingContext.HostingEnvironment.IsDevelopment()))
                .UseConsoleLifetime()
                .ConfigureServices((hostContext, services) => {
                    services.AddLogging(configure => configure.AddSimpleConsole(o => o.SingleLine = true));

                    if (hostContext.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {
                        services.AddOpenTelemetry()
                            .ConfigureResource(resource =>
                                resource.AddService($"{name}-{hostContext.HostingEnvironment.ApplicationName}"))
                            .WithMetrics(metricsConfig =>
                                metricsConfig
                                    .AddRuntimeInstrumentation()
                                    .AddMeter([
                                        "Microsoft.AspNetCore.Hosting",
                                        "Microsoft.AspNetCore.Server.Kestrel",
                                        "System.Net.Http",
                                        $"{typeof(TracingMiddleware<,>).FullName}",
                                    ])
                                    .AddPrometheusExporter())
                            .WithTracing(traceConfig =>
                                traceConfig
                                    .SetSampler<AlwaysOnSampler>()
                                    .AddHttpClientInstrumentation()
                                    .AddSource([
                                        $"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware",
                                        $"{typeof(StripeService).FullName}"
                                    ]))
                            .WithLogging();

                        services.ConfigureOpenTelemetryTracerProvider(o =>
                            o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
                        services.ConfigureOpenTelemetryLoggerProvider(o =>
                            o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
                    }

                    services.AddWarpCacheClient(
                        hostContext.Configuration.GetCacheServerAddress(),
                        StringMessageSerializerContext.Default.String);

                    services.AddSingleton(TimeProvider.System);
                    services.AddSingleton<ICacheStore<string, string>, MemoryCacheStore<string, string>>();
                    services.AddSingleton<IEvictionPolicy<string>>(new LruEvictionPolicy<string>(1000));
                    services.AddSingleton<ChannelCacheMediator<string, string>>();

                    services.Configure<OidcConfig>(hostContext.Configuration.GetSection("Oidc"));

                    services.AddHttpClient<IServerTokenClient, ServerTokenClient>()
                        .ConfigureHttpClient(client => client.BaseAddress = new Uri(hostContext.Configuration.GetOidcServerUrl()));
                    services.AddNudgesClient()
                        .ConfigureHttpClient((sp, client) => {
                            var config = sp.GetRequiredService<IConfiguration>();
                            client.BaseAddress = new Uri(config.GetGraphQLApiUrl());
                            using var scope = sp.CreateScope();
                            var token = scope.ServiceProvider.GetRequiredService<IServerTokenClient>()
                                .GetTokenAsync("kafka-consumer").ConfigureAwait(false).GetAwaiter().GetResult();
                            token.Match(token => {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                                // TODO: this throw is intentional.  It should break the startup.
                            }, e => throw new Exception(e.ErrorDescription));
                        });
                    services.AddSingleton<Func<INudgesClient>>(static sp => sp.GetRequiredService<INudgesClient>);
                    services.AddLocalizationClient((sp, o) => {
                        o.ServerAddress = sp.GetRequiredService<IConfiguration>().GetLocalizationApiUrl();
                        return o;
                    });
                });

internal static class HandlerBuilders {
    public static IHostBuilder ConfigureNotificationHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                if (hostContext.HostingEnvironment.IsDevelopment()) {
                    services.AddSingleton<INotifier, LocalNotifier>();
                } else {
                    services.AddSingleton<Func<ITwilioRestClient>>(static sp => {
                        var config = sp.GetRequiredService<IConfiguration>();
                        TwilioClient.Init(config.GetTwilioAccountSid(),
                           config.GetTwilioAuthToken());
                        return TwilioClient.GetRestClient;
                    });
                    services.AddSingleton<INotifier, TwilioNotifier>();
                }

                services.AddTransient<NotificationMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<NotificationKey, NotificationEvent>(
                            Topics.Notifications,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<NotificationKey, NotificationEvent>())
                        .Use(sp.GetRequiredService<NotificationMessageMiddleware>())
                        .Build());

                services.AddHostedService<MessageHandlerService<NotificationKey, NotificationEvent>>();
            });
    public static IHostBuilder ConfigurePlanSubscriptionHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));
                services.AddSingleton<KafkaMessageProducer<ClientKey, ClientEvent>>(static sp =>
                    new ClientEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>, PlanSubscriptionEventMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PlanSubscriptionKey, PlanSubscriptionEvent>(
                            Topics.PlanSubscriptions,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PlanSubscriptionKey, PlanSubscriptionEvent>>();
            });
    public static IHostBuilder ConfigureClientHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddKeycloakAdminHttpClient(hostContext.Configuration);

                services.AddTransient<IMessageMiddleware<ClientKey, ClientEvent>, ClientMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<ClientKey, ClientEvent>(
                            Topics.Clients,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<ClientKey, ClientEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<ClientKey, ClientEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<ClientKey, ClientEvent>>();
            });
    public static IHostBuilder ConfigureUserAuthenticationHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));
                services.AddSingleton<KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent>>(static sp =>
                    new DeadLetterEventProducer(Topics.DeadLetter, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>, UserAuthenticationMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<UserAuthenticationEventKey, UserAuthenticationEvent>(
                            Topics.UserAuthentication,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>())
                        .Use(new SmartErrorHandlingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>(
                            sp.GetRequiredService<ILogger<SmartErrorHandlingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>>>(),
                            sp.GetRequiredService<KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent>>()
                        ))
                        .Use(sp.GetRequiredService<IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<UserAuthenticationEventKey, UserAuthenticationEvent>>();
            });
    public static IHostBuilder ConfigurePaymentEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IMessageMiddleware<PaymentKey, PaymentEvent>, PaymentMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PaymentKey, PaymentEvent>(
                            Topics.Payments,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PaymentKey, PaymentEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PaymentKey, PaymentEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PaymentKey, PaymentEvent>>();
            });

    public static IHostBuilder ConfigurePlanEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton(static sp =>
                    new PriceTierEventProducer(Topics.PriceTiers, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PlanKey, PlanEvent>, PlanMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PlanKey, PlanEvent>(
                            Topics.Plans,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PlanKey, PlanEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PlanKey, PlanEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PlanKey, PlanEvent>>();
            });

    public static IHostBuilder ConfigurePriceTierEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PriceTierEventKey, PriceTierEvent>, PriceTierMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PriceTierEventKey, PriceTierEvent>(
                            Topics.PriceTiers,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PriceTierEventKey, PriceTierEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PriceTierEventKey, PriceTierEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PriceTierEventKey, PriceTierEvent>>();
            });
}
