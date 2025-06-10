using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.Data.Payments;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;

namespace DbSeeder;

internal class DbSeedService(ILogger<DbSeedService> logger,
                             IDbContextFactory<UserDbContext> userDbContextFactory,
                             IDbContextFactory<PaymentDbContext> paymentDbContextFactory,
                             IHostApplicationLifetime appLifetime) : IHostedService {

    private const string DefaultClientPhone = "+15555555555";

    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreDefaultClient(cancellationToken);
        await StoreDefaultAdmin(cancellationToken);
        await StoreMerchantServicesClient("Debug", -1, cancellationToken);
        await StoreMerchantServicesClient("Stripe", 1, cancellationToken);

        appLifetime.StopApplication();
    }

    private async Task StoreDefaultClient(CancellationToken cancellationToken) {
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var defaultClient = context.Clients.Where(c => c.PhoneNumber == DefaultClientPhone).FirstOrDefault();
        if (defaultClient is null) {
            var newClient = context.Clients.Add(new Client {
                Locale = "en-US",
                Name = "Nudges",
                PhoneNumber = DefaultClientPhone,
                Slug = "test"
            });
            await context.SaveChangesAsync(cancellationToken);
            logger.LogStoredDefaultClient(newClient.Entity.Id);
        }
    }

    private async Task StoreMerchantServicesClient(string name, int id, CancellationToken cancellationToken) {
        await using var context = await paymentDbContextFactory.CreateDbContextAsync(cancellationToken);
        var defaultClient = context.MerchantServices.Where(c => c.Id == id).FirstOrDefault();
        if (defaultClient is null) {
            var newClient = context.MerchantServices.Add(new Nudges.Data.Payments.Models.MerchantService {
                Id = id,
                Name = name
            });
            await context.SaveChangesAsync(cancellationToken);

            logger.LogStoredMerchantService(newClient.Entity.Id);
        }
    }

    private async Task StoreDefaultAdmin(CancellationToken cancellationToken) {
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var defaultClient = context.Clients.Where(c => c.PhoneNumber == DefaultClientPhone).FirstOrDefault();
        if (defaultClient is null) {
            logger.LogDefaultClientNotFound(DefaultClientPhone);
            return;
        }
        var defaultAdmin = context.Admins.Where(c => c.Id == defaultClient.Id).FirstOrDefault();
        if (defaultAdmin is null) {
            var newAdmin = context.Admins.Add(new Admin {
                Id = defaultClient.Id,
            });
            await context.SaveChangesAsync(cancellationToken);
            logger.LogStoredDefaultAdmin(newAdmin.Entity.Id);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not find client with phone number {PhoneNumber}")]
    public static partial void LogDefaultClientNotFound(this ILogger<DbSeedService> logger, string phoneNumber);
}
