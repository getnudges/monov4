using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Stripe;
using Nudges.Redis;

namespace DbSeeder;

internal sealed class RedisSeedService(ILogger<RedisSeedService> logger,
                           IStripeClient stripe,
                           IConnectionMultiplexer redis,
                           IHostApplicationLifetime appLifetime) : IHostedService {

    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreProducts(cancellationToken);

        appLifetime.StopApplication();
    }

    private async Task StoreProducts(CancellationToken cancellationToken) {
        var productService = new ProductService(stripe);
        var priceService = new PriceService(stripe);

        try {
            var products = await productService.ListAsync(new ProductListOptions {
                Limit = 10, // TODO: how do I decide how many to get?
                Active = true,
                Type = "service"
            }, cancellationToken: cancellationToken);

            var db = redis.GetDatabase();
            logger.LogConnectedToRedis(redis.GetEndPoints().First());
            foreach (var product in products) {
                var prices = await priceService.ListAsync(new PriceListOptions {
                    Product = product.Id
                }, cancellationToken: cancellationToken);
                foreach (var price in prices) {
                    db.StorePrice(price.Id, product.Name, product.Description ?? string.Empty);
                    db.SetPriceLimits(price.Id, price.Metadata);
                    logger.LogStoredPriceLimits(price.Id, product.Name);
                }
            }
        } catch (Exception ex) {
            logger.LogException(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        logger.LogServiceStopping();
        return Task.CompletedTask;
    }
}

internal static class RedisSeedServiceLogs {
    public static class EventIds {
        public const int ServiceStopping = 1000;
        public const int ServiceStarting = 1010;
        public const int ConnectedToRedis = 2000;
        public const int StorePrice = 2010;
        public const int StoredDefaultClient = 3000;
        public const int Exception = 3010;
    }

    public static readonly Action<ILogger<RedisSeedService>, Exception?> ServiceStopping =
        LoggerMessage.Define(LogLevel.Information, new EventId(EventIds.ServiceStopping, nameof(ServiceStopping)), "RedisSeedService is stopping.");

    public static readonly Action<ILogger<RedisSeedService>, Exception?> ServiceStarting =
        LoggerMessage.Define(LogLevel.Information, new EventId(EventIds.ServiceStarting, nameof(ServiceStarting)), "RedisSeedService is starting.");

    public static readonly Action<ILogger<RedisSeedService>, System.Net.EndPoint, Exception?> ConnectedToRedis =
        LoggerMessage.Define<System.Net.EndPoint>(LogLevel.Information, new EventId(EventIds.ConnectedToRedis, nameof(ConnectedToRedis)), "Connected to Redis at {Endpoint}");

    public static readonly Action<ILogger<RedisSeedService>, string, string, Exception?> StorePrice =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(EventIds.StorePrice, nameof(StorePrice)), "Stored price {Id}: (Product: {Name})");

    public static readonly Action<ILogger<RedisSeedService>, Guid, Exception?> StoredDefaultClient =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(EventIds.StoredDefaultClient, nameof(StoredDefaultClient)), "Stored default client with Id {Id}");

    public static readonly Action<ILogger<RedisSeedService>, Exception?> Exception =
        LoggerMessage.Define(LogLevel.Error, new EventId(EventIds.Exception, nameof(Exception)), "Error storing price");

    public static void LogServiceStopping(this ILogger<RedisSeedService> logger) =>
        ServiceStopping(logger, null);
    public static void LogServiceStarting(this ILogger<RedisSeedService> logger) =>
        ServiceStarting(logger, null);
    public static void LogConnectedToRedis(this ILogger<RedisSeedService> logger, System.Net.EndPoint endpoint) =>
        ConnectedToRedis(logger, endpoint, null);
    public static void LogStoredPriceLimits(this ILogger<RedisSeedService> logger, string id, string name) =>
        StorePrice(logger, id, name, null);
    public static void LogStoredDefaultClient(this ILogger<RedisSeedService> logger, Guid id) =>
        StoredDefaultClient(logger, id, null);
    public static void LogException(this ILogger<RedisSeedService> logger, Exception ex) =>
        Exception(logger, ex);
}
