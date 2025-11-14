using System.CommandLine;
using System.Net.Http.Headers;
using KafkaConsumer;
using KafkaConsumer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Kafka.Middleware;
using Nudges.Localization.Client;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Precision.WarpCache;
using Precision.WarpCache.Grpc.Client;
using Precision.WarpCache.MemoryCache;

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
                        .ConfigurePrimaryHttpMessageHandler(sp => {
                            var env = sp.GetRequiredService<IHostEnvironment>();
                            return env.IsDevelopment()
                                ? new HttpClientHandler {
                                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                } : new HttpClientHandler();
                        })
                        .ConfigureHttpClient(client => {
                            client.BaseAddress = new Uri(hostContext.Configuration.GetOidcServerUrl());
                        });
                    services.AddNudgesClient()
                        .ConfigureHttpClient((sp, client) => {
                            var config = sp.GetRequiredService<IConfiguration>();
                            client.BaseAddress = new Uri(config.GetGraphQLApiUrl());
                            using var scope = sp.CreateScope();
                            var token = scope.ServiceProvider.GetRequiredService<IServerTokenClient>()
                                .GetTokenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
