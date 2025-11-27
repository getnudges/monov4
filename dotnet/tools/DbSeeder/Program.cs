using System.CommandLine;
using DbSeeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nudges.Data;
using Nudges.Data.Payments;
using Nudges.Data.Security;
using Nudges.Data.Users;
using Nudges.Security;
using StackExchange.Redis;
using Stripe;

static IHostBuilder CreateBaseHost(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
            config
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables())
        .UseConsoleLifetime();

static IHostBuilder CreateDbHostBuilder(string[] args) =>
    CreateBaseHost(args)
        .ConfigureServices(static (hostContext, services) => {
            var config = hostContext.Configuration;
            services.AddLogging(configure => configure.AddSimpleConsole(o => o.SingleLine = true));

            services.Configure<HashSettings>(config.GetSection("HashSettings"));
            services.AddOptions<HashSettings>(nameof(HashSettings));
            services.Configure<EncryptionSettings>(config.GetSection("EncryptionSettings"));
            services.AddOptions<EncryptionSettings>(nameof(EncryptionSettings));
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

            services.AddDbContextFactory<PaymentDbContext>(static (sp, o) =>
                o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString(DbConstants.PaymentDb), static pg => {
                    pg.EnableRetryOnFailure(3);
                    pg.CommandTimeout(30);
                }));

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

            services.AddHostedService<DbSeedService>();
        });

static IHostBuilder CreateRedisHostBuilder(string[] args) =>
    CreateBaseHost(args)
        .ConfigureServices(static (hostContext, services) => {
            var config = hostContext.Configuration;
            services.AddLogging(configure => configure.AddConsole());
            services.AddSingleton<IConnectionMultiplexer>((s) =>
                ConnectionMultiplexer.Connect(
                    ConfigurationOptions.Parse(config.GetRedisUrl())));
            services.AddSingleton<IStripeClient>(s =>
                new StripeClient(config.GetStripeApiKey()));

            services.AddHostedService<RedisSeedService>();
        });

var redisCommand = new Command("redis");
var dbCommand = new Command("db");

var rootCommand = new RootCommand {
    redisCommand,
    dbCommand
};

redisCommand.SetAction(async r => await CreateRedisHostBuilder(args).Build().RunAsync());
dbCommand.SetAction(async r => await CreateDbHostBuilder(args).Build().RunAsync());

await rootCommand.Parse(args).InvokeAsync();
