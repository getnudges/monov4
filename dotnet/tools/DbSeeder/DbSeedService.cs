using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;
using Nudges.Security;

namespace DbSeeder;

internal sealed class DbSeedService(ILogger<DbSeedService> logger,
                             IDbContextFactory<UserDbContext> userDbContextFactory,
                             HashService hashService,
                             IHostApplicationLifetime appLifetime) : IHostedService {

    private const string DefaultClientPhone = "+15555555555";

    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreDefaultClient(cancellationToken);
        //await StoreMerchantServicesClient("Debug", -1, cancellationToken);
        //await StoreMerchantServicesClient("Stripe", 1, cancellationToken);

        appLifetime.StopApplication();
    }

    //private async Task StoreMerchantServicesClient(string name, int id, CancellationToken cancellationToken) {
    //    await using var context = await paymentDbContextFactory.CreateDbContextAsync(cancellationToken);
    //    var defaultClient = context.MerchantServices.Where(c => c.Id == id).FirstOrDefault();
    //    if (defaultClient is null) {
    //        var newClient = context.MerchantServices.Add(new Nudges.Data.Payments.Models.MerchantService {
    //            Id = id,
    //            Name = name
    //        });
    //        await context.SaveChangesAsync(cancellationToken);

    //        logger.LogStoredMerchantService(newClient.Entity.Id);
    //    }
    //}

    private async Task StoreDefaultClient(CancellationToken cancellationToken) {
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(DefaultClientPhone));
        var defaultUser = await context.Users.Include(u => u.Client)
            .SingleOrDefaultAsync(c => c.PhoneNumberHash == phoneNumberHash, cancellationToken);
        if (defaultUser is not { } user) {
            throw new Exception("Default Admin user not found. Make sure to create the default admin user before seeding the default client.");
        }
        if (defaultUser?.Client is not { } client) {
            var defaultClient = context.Clients.Where(c => c.Id == user.Id).FirstOrDefault();
            if (defaultClient is null) {
                var newClient = context.Clients.Add(new Client {
                    Id = user.Id,
                    Slug = "nudges",
                    Name = "Nudges"
                });
                await context.SaveChangesAsync(cancellationToken);
                logger.LogStoredDefaultAdmin(newClient.Entity.Id);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        logger.LogServiceStopping();
        return Task.CompletedTask;
    }
}

internal static partial class DbSeedServiceLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "DbSeedService is stopping.")]
    public static partial void LogServiceStopping(this ILogger<DbSeedService> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "DbSeedService is starting.")]
    public static partial void LogServiceStarting(this ILogger<DbSeedService> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to Redis at {Endpoint}")]
    public static partial void LogConnectedToRedis(this ILogger<DbSeedService> logger, System.Net.EndPoint endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored price {Id}: (Product: {Name})")]
    public static partial void LogStoredPriceLimits(this ILogger<DbSeedService> logger, string id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored default client with Id {Id}")]
    public static partial void LogStoredDefaultClient(this ILogger<DbSeedService> logger, Guid id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored Merchant Service with Id {Id}")]
    public static partial void LogStoredMerchantService(this ILogger<DbSeedService> logger, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored default admin with Id {Id}")]
    public static partial void LogStoredDefaultAdmin(this ILogger<DbSeedService> logger, Guid? id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error storing price")]
    public static partial void LogException(this ILogger<DbSeedService> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Shopify merchant service stored with Id {Id}")]
    public static partial void LogStoredShopifyMerchantService(this ILogger<DbSeedService> logger, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not find admin with phone number {PhoneNumber}")]
    public static partial void LogDefaultClientNotFound(this ILogger<DbSeedService> logger, string phoneNumber);
}
