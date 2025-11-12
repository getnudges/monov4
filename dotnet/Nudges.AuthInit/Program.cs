using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.AuthInit;
using Nudges.Data.Users;
using Nudges.Configuration.Extensions;
using Nudges.Auth;
using Nudges.Auth.Keycloak;

static IHostBuilder CreateBaseHost(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
            config
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables())
        .UseConsoleLifetime();

static IHostBuilder CreateDbHostBuilder(string[] args) =>
    CreateBaseHost(args)
        .ConfigureServices((hostContext, services) => {
            services.AddLogging(static configure => configure.AddSimpleConsole(o => o.SingleLine = true));
            services.Configure<OidcConfig>(hostContext.Configuration.GetSection("Oidc"));
            services.AddDbContextFactory<UserDbContext>(static (sp, o) =>
                o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetUserDbConnectionString(), o => {
                    o.EnableRetryOnFailure(3);
                    o.CommandTimeout(30);
                }));

            services.AddHttpClient<IOidcClient, KeycloakOidcClient>((sp, client) => {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var baseUrl = configuration.GetOidcServerUrl();
                client.BaseAddress = new Uri(baseUrl);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddHostedService<AuthInitService>();
        });

var dbCommand = new Command("seed");

var rootCommand = new RootCommand {
    dbCommand
};

dbCommand.SetAction(async r => await CreateDbHostBuilder(args).Build().RunAsync());

await rootCommand.Parse(args).InvokeAsync();
