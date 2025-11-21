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
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Nudges.Localization.Client;
using Nudges.Telemetry;
using Precision.WarpCache;
using Precision.WarpCache.Grpc.Client;
using Precision.WarpCache.MemoryCache;

var notificationsCmd = new Command(Topics.Notifications);
var plansCmd = new Command(Topics.Plans);
var priceTierCmd = new Command(Topics.PriceTiers);
var planSubscriptionsCmd = new Command(Topics.PlanSubscriptions);
var paymentsCmd = new Command(Topics.Payments);
var clientsCmd = new Command(Topics.Clients);
var userAuthCmd = new Command(Topics.UserAuthentication);
var foreignProductCmd = new Command(Topics.ForeignProducts);

notificationsCmd.SetAction(r => CreateBaseHost(args, Topics.Notifications).ConfigureNotificationHandler().Build().RunAsync());
plansCmd.SetAction(r => CreateBaseHost(args, Topics.Plans).ConfigurePlanEventHandler().Build().RunAsync());
priceTierCmd.SetAction(r => CreateBaseHost(args, Topics.PriceTiers).ConfigurePriceTierEventHandler().Build().RunAsync());
paymentsCmd.SetAction(r => CreateBaseHost(args, Topics.Payments).ConfigurePaymentEventHandler().Build().RunAsync());
planSubscriptionsCmd.SetAction(r => CreateBaseHost(args, Topics.PlanSubscriptions).ConfigurePlanSubscriptionHandler().Build().RunAsync());
clientsCmd.SetAction(r => CreateBaseHost(args, Topics.Clients).ConfigureClientHandler().Build().RunAsync());
userAuthCmd.SetAction(r => CreateBaseHost(args, Topics.UserAuthentication).ConfigureUserAuthenticationHandler().Build().RunAsync());
foreignProductCmd.SetAction(r => CreateBaseHost(args, Topics.ForeignProducts).ConfigureForeignProductEventHandler().Build().RunAsync());

var rootCommand = new RootCommand {
    notificationsCmd,
    plansCmd,
    priceTierCmd,
    paymentsCmd,
    planSubscriptionsCmd,
    clientsCmd,
    userAuthCmd,
    foreignProductCmd,
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
                    hostingContext.Configuration.GetValue("FILEMAP", string.Empty), false))
                .UseConsoleLifetime()
                .ConfigureServices((hostContext, services) => {
                    services.AddLogging(configure => configure.AddSimpleConsole(o => o.SingleLine = true));

                    if (hostContext.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {
                        services.AddOpenTelemetryConfiguration(
                            url,
                            $"{name}-{hostContext.HostingEnvironment.ApplicationName}", [
                                "Microsoft.AspNetCore.Hosting",
                                "Microsoft.AspNetCore.Server.Kestrel",
                                "System.Net.Http",
                                $"{typeof(TracingMiddleware<,>).FullName}",
                            ], [
                                $"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware",
                                $"{typeof(StripeService).FullName}"
                            ]);
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
                        .ConfigureHttpClient(client =>
                            client.BaseAddress = new Uri(hostContext.Configuration.GetOidcServerUrl()));
                    services.AddNudgesClient()
                        .ConfigureHttpClient((sp, client) => {
                            var config = sp.GetRequiredService<IConfiguration>();
                            client.BaseAddress = new Uri(config.GetGraphQLApiUrl());
                            using var scope = sp.CreateScope();
                            var tokenResult = scope.ServiceProvider.GetRequiredService<IServerTokenClient>()
                                .GetTokenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            // TODO: I need to do something significant if I can't get the token.
                            tokenResult.Match(token => {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                                // TODO: this throw is intentional.  It should break the startup.
                            }, e => throw e);
                        });
                    services.AddSingleton<Func<INudgesClient>>(static sp => sp.GetRequiredService<INudgesClient>);
                    services.AddLocalizationClient((sp, o) => {
                        o.ServerAddress = sp.GetRequiredService<IConfiguration>().GetLocalizationApiUrl();
                        return o;
                    });
                });
