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
using Microsoft.Extensions.Options;
using Nudges.Auth;
using Nudges.Configuration;
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
                if (configure.ApplicationServices.GetRequiredService<IOptions<OtlpSettings>>().Value?.Endpoint is not null) {
                    configure.UseRouting().UseEndpoints(e => e.MapPrometheusScrapingEndpoint());
                }
            });
        })
        .ConfigureAppConfiguration((hostingContext, config) =>
            config.AddEnvironmentVariables().AddUserSecrets<Program>())
        .ConfigureServices((hostContext, services) => {
            var settings = new Settings();
            hostContext.Configuration.Bind(settings);
            services.Configure<OidcSettings>(hostContext.Configuration.GetSection(nameof(Settings.Oidc)));
            services.Configure<OidcConfig>(hostContext.Configuration.GetSection(nameof(Settings.Oidc)));
            services.Configure<KafkaSettings>(hostContext.Configuration.GetSection(nameof(Settings.Kafka)));
            services.Configure<OtlpSettings>(hostContext.Configuration.GetSection(nameof(Settings.Otlp)));
            services.AddLogging(configure => configure.AddSimpleConsole(o => o.SingleLine = true));

            if (settings.Otlp.Endpoint is string url) {
                services.AddOpenTelemetryConfiguration<Program>(
                    url,
                    $"{name}-{hostContext.HostingEnvironment.ApplicationName}", [
                        "Microsoft.AspNetCore.Hosting",
                                "Microsoft.AspNetCore.Server.Kestrel",
                                "System.Net.Http",
                                $"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware",
                    ], [
                        $"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware",
                                $"{typeof(StripeService).Namespace}.StripeService",
                                $"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware",
                                $"{typeof(RetryMiddleware<,>).Namespace}.RetryMiddleware",
                                $"{typeof(CircuitBreakerMiddleware<,>).Namespace}.CircuitBreaker",
                    ], null, null, o => o.Filter = ctx => ctx.Request.Method == "POST");
            }

            services.AddWarpCacheClient(
                settings.WarpCache,
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
                    client.BaseAddress = new Uri(settings.Oidc.ServerUrl));

            services.AddHttpContextAccessor();
            services.AddScoped<AuthenticationDelegatingHandler>();
            services.AddHttpClient<INudgesClient>(NudgesClient.ClientName)
                .AddHttpMessageHandler<AuthenticationDelegatingHandler>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                    sp.GetRequiredService<IHostEnvironment>().IsDevelopment()
                        ? new HttpClientHandler {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        } : new HttpClientHandler());
            services.AddNudgesClient()
                .ConfigureHttpClient((sp, client) => {
                    var config = sp.GetRequiredService<IConfiguration>();
                    client.BaseAddress = new Uri(config.GetGraphQLApiUrl());
                });
            services.AddSingleton<Func<INudgesClient>>(static sp => sp.GetRequiredService<INudgesClient>);
            services.AddLocalizationClient((sp, o) => {
                o.ServerAddress = sp.GetRequiredService<IConfiguration>().GetLocalizationApiUrl();
                return o;
            });
        });
