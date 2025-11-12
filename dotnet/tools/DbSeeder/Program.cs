using System.CommandLine;
using DbSeeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Stripe;
using Nudges.Data.Payments;
using Nudges.Data.Users;

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
            var config = hostContext.Configuration;
            services.AddLogging(configure => configure.AddSimpleConsole(o => o.SingleLine = true));
            services.AddDbContextFactory<UserDbContext>((c, o) =>
                o.UseNpgsql(config.GetConnectionString(AppConfiguration.ConnectionStrings.UserDb), o => {
                    o.EnableRetryOnFailure(3);
                    o.CommandTimeout(30);
                }));
            services.AddDbContextFactory<PaymentDbContext>((c, o) =>
                o.UseNpgsql(config.GetConnectionString(AppConfiguration.ConnectionStrings.PaymentDb), o => {
                    o.EnableRetryOnFailure(3);
                    o.CommandTimeout(30);
                }));

            services.AddHostedService<DbSeedService>();
        });

static IHostBuilder CreateRedisHostBuilder(string[] args) =>
    CreateBaseHost(args)
        .ConfigureServices((hostContext, services) => {
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
