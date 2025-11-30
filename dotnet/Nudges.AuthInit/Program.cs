using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nudges.Auth;
using Nudges.Auth.Keycloak;
using Nudges.AuthInit;
using Nudges.Data;
using Nudges.Data.Security;
using Nudges.Data.Users;
using Nudges.Security;

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
            // we have to use the admin creds because at this point client configs aren't exported yet
            services.Configure<OidcConfig>(hostContext.Configuration.GetSection("Oidc"));
            services.Configure<HashSettings>(hostContext.Configuration.GetSection("HashSettings"));
            services.AddOptions<HashSettings>(nameof(HashSettings));
            services.Configure<EncryptionSettings>(hostContext.Configuration.GetSection("EncryptionSettings"));
            services.AddOptions<EncryptionSettings>(nameof(EncryptionSettings));
            var settings = new Settings();
            hostContext.Configuration.Bind(settings);
            services.AddSingleton(static sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var base64 = config["HashSettings:HashKeyBase64"]
                    ?? throw new InvalidOperationException("Missing HashSettings:HashKeyBase64");

                var keyBytes = Convert.FromBase64String(base64);
                return new HashService(
                    Options.Create(new HashSettings { HashKey = keyBytes })
                );
            });
            services.AddSingleton<IEncryptionService>(static sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var base64 = config["EncryptionSettings:Key"]
                    ?? throw new InvalidOperationException("Missing EncryptionSettings:Key");

                var keyBytes = Convert.FromBase64String(base64);
                return new AesGcmEncryptionService(
                    Options.Create(new EncryptionSettings { Key = keyBytes })
                );
            });
            services.AddSingleton<HashingSaveChangesInterceptor>();
            services.AddSingleton<EncryptionSaveChangesInterceptor>();
            services.AddSingleton<EncryptionMaterializationInterceptor>();

            services.AddDbContextFactory<UserDbContext>(static (sp, o) => {
                o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString(DbConstants.UserDb), static pg => {
                    pg.EnableRetryOnFailure(3);
                    pg.CommandTimeout(30);
                });
                o.EnableDetailedErrors();

                o.AddInterceptors(
                    sp.GetRequiredService<EncryptionSaveChangesInterceptor>(),
                    sp.GetRequiredService<EncryptionMaterializationInterceptor>(),
                    sp.GetRequiredService<HashingSaveChangesInterceptor>()
                );
            });

            services.AddHttpClient<IOidcClient, KeycloakOidcClient>((sp, client) =>
                client.BaseAddress = new Uri(settings.Oidc.ServerUrl))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
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
