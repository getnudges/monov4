using BenchmarkDotNet.Attributes;
using Bogus;
using Grpc.Net.Client;
using Precision.WarpCache.Grpc.Client;

namespace Precision.WarpCache.Demo;

public static class ObjectMaker {
    public static readonly Faker<Customer> CustomerMaker = CreateCustomerFaker();

    public static Faker<Customer> CreateCustomerFaker() {

        var addressFaker = new Faker<Address>()
            .RuleFor(a => a.Street, f => f.Address.StreetAddress())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.State, f => f.Address.State())
            .RuleFor(a => a.ZipCode, f => f.Address.ZipCode())
            .RuleFor(a => a.Country, f => f.Address.Country());

        var supplierFaker = new Faker<Supplier>()
            .RuleFor(s => s.SupplierId, f => f.Random.Int(100, 999))
            .RuleFor(s => s.CompanyName, f => f.Company.CompanyName())
            .RuleFor(s => s.ContactEmail, f => f.Internet.Email());

        var reviewFaker = new Faker<Review>()
            .RuleFor(r => r.Rating, f => f.Random.Int(1, 5))
            .RuleFor(r => r.Comment, f => f.Lorem.Sentence())
            .RuleFor(r => r.Reviewer, f => f.Name.FullName());

        var productFaker = new Faker<Product>()
            .RuleFor(p => p.ProductId, f => f.Random.Int(1, 10000))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Supplier, f => supplierFaker.Generate())
            .RuleFor(p => p.Reviews, f => reviewFaker.Generate(f.Random.Int(1, 5)));


        var priceDetailsFaker = new Faker<PriceDetails>()
            .RuleFor(p => p.UnitPrice, f => f.Random.Decimal(5, 500))
            .RuleFor(p => p.Discount, f => f.Random.Decimal(0, 0.3M));

        var orderItemFaker = new Faker<OrderItem>()
            .RuleFor(oi => oi.Quantity, f => f.Random.Int(1, 10))
            .RuleFor(oi => oi.Product, f => productFaker.Generate())
            .RuleFor(oi => oi.Price, f => priceDetailsFaker.Generate());

        var paymentFaker = new Faker<PaymentInfo>()
            .RuleFor(p => p.PaymentMethod, f => f.PickRandom("Credit Card", "PayPal", "Bank Transfer"))
            .RuleFor(p => p.Successful, f => f.Random.Bool());

        var shippingFaker = new Faker<ShippingInfo>()
            .RuleFor(s => s.Carrier, f => f.PickRandom("UPS", "FedEx", "DHL"))
            .RuleFor(s => s.TrackingNumber, f => f.Random.Replace("####-####-####"))
            .RuleFor(s => s.EstimatedDelivery, f => f.Date.Future(1));

        var orderFaker = new Faker<Order>()
            .RuleFor(o => o.OrderId, f => Guid.NewGuid())
            .RuleFor(o => o.OrderDate, f => f.Date.Past(2))
            .RuleFor(o => o.Items, f => orderItemFaker.Generate(f.Random.Int(1, 5)))
            .RuleFor(o => o.Payment, f => paymentFaker.Generate())
            .RuleFor(o => o.Shipping, f => shippingFaker.Generate());

        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => f.IndexFaker)
            .RuleFor(c => c.FullName, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Address, f => addressFaker.Generate())
            .RuleFor(c => c.Orders, f => orderFaker.Generate(f.Random.Int(1, 3)));

        return customerFaker;
    }
}

public class CacheClientBenchmark {
    private readonly CacheClient<Customer> _client;

    public CacheClientBenchmark() {
        var host = "localhost";
        var port = 7777;
        var handler = new SocketsHttpHandler {
            KeepAlivePingDelay = TimeSpan.FromSeconds(10),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            EnableMultipleHttp2Connections = true
        };
        var channel = GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions {
            HttpHandler = handler
        });
        _client = new CacheClient<Customer>(channel, CustomerMessageSerializerContext.Default.Customer);
    }

    [Benchmark]
    public async Task SetAndRetrieve() {
        var value = ObjectMaker.CustomerMaker.Generate();
        var key = value.Id.ToString();
        var result = await _client.SetAsync(key, value);
        var retrievedValue = await _client.GetAsync(key);
    }

    [Benchmark]
    public async Task JustSet() {
        var value = ObjectMaker.CustomerMaker.Generate();

        var result = await _client.SetAsync(value.Id.ToString(), value);
    }

    [Benchmark]
    public async Task JustRetrieve() {
        var key = "example";

        var retrievedValue = await _client.GetAsync(key);
    }
}

public class CacheClientSetBenchmark {
    private readonly CacheClient<Customer> _client;

    public CacheClientSetBenchmark() {
        var host = "localhost";
        var port = 7777;
        var handler = new SocketsHttpHandler {
            KeepAlivePingDelay = TimeSpan.FromSeconds(10),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            EnableMultipleHttp2Connections = true
        };
        var channel = GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions {
            HttpHandler = handler
        });
        _client = new CacheClient<Customer>(channel, CustomerMessageSerializerContext.Default.Customer);
    }

    [Benchmark]
    public async Task JustSet() {
        var value = ObjectMaker.CustomerMaker.Generate();

        var result = await _client.SetAsync(value.Id.ToString(), value);
    }
}

public class CacheClientGetBenchmark {
    private readonly CacheClient<Customer> _client;

    public CacheClientGetBenchmark() {
        var host = "localhost";
        var port = 7777;
        var handler = new SocketsHttpHandler {
            KeepAlivePingDelay = TimeSpan.FromSeconds(10),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            EnableMultipleHttp2Connections = true
        };
        var channel = GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions {
            HttpHandler = handler
        });
        _client = new CacheClient<Customer>(channel, CustomerMessageSerializerContext.Default.Customer);
    }

    [Benchmark]
    public async Task JustRetrieve() {
        var key = "example";

        var retrievedValue = await _client.GetAsync(key);
    }
}
