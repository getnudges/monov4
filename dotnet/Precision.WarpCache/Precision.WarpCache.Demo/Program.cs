using System.Diagnostics;
using BenchmarkDotNet.Running;
using Grpc.Net.Client;
using Precision.WarpCache.Grpc.Client;

namespace Precision.WarpCache.Demo;

public sealed class Program {
    public static async Task Main(string[] args) {
        //BenchmarkRunner.Run<ChannelCacheMediatorBenchmark>();
        //BenchmarkRunner.Run<CacheClientBenchmark>();
        //BenchmarkRunner.Run<CacheClientSetBenchmark>();
        //BenchmarkRunner.Run<CacheClientGetBenchmark>();
        await TestCache();
        await Task.CompletedTask;
    }

    private static async Task TestCache() {
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
        var customerClient = new CacheClient<Customer>(channel, CustomerMessageSerializerContext.Default.Customer);
        //var stringClient = new CacheClient<string, string>(channel, StringMessageSerializerContext.Default);
        //await TestSetGet(stringClient);
        //await TestSetExpire(customerClient);

        Console.WriteLine("Sequential:");
        await TestSequential(customerClient);
        Console.WriteLine("Parallel:");
        await TestParallel(customerClient);
    }

    private static async Task TestSequential(CacheClient<Customer> customerClient) {
        var customers = GetCustomers(10000);
        Stopwatch setWatch = new();
        setWatch.Start();
        foreach (var customer in customers) {
            await customerClient.SetAsync(customer.Id.ToString(), customer);
        }
        setWatch.Stop();
        Console.WriteLine($"Took {setWatch.ElapsedMilliseconds}ms to load");

        Stopwatch getWatch = new();
        getWatch.Start();
        foreach (var customer in customers) {
            await customerClient.GetAsync(customer.Id.ToString());
        }
        getWatch.Stop();
        Console.WriteLine($"Took {getWatch.ElapsedMilliseconds}ms to iterate");
    }
    private static async Task TestParallel(CacheClient<Customer> customerClient) {
        var customers = GetCustomers(10000);
        Stopwatch setWatch = new();
        setWatch.Start();
        await Task.WhenAll(customers.Select(c => customerClient.SetAsync(c.Id.ToString(), c)));
        setWatch.Stop();
        Console.WriteLine($"Took {setWatch.ElapsedMilliseconds}ms to load");

        Stopwatch getWatch = new();
        getWatch.Start();
        await Task.WhenAll(customers.Select(c => customerClient.GetAsync(c.Id.ToString())));
        getWatch.Stop();
        Console.WriteLine($"Took {getWatch.ElapsedMilliseconds}ms to iterate");
    }

    private static IEnumerable<Customer> GetCustomers(int count) {
        for (var i = 0; i < count; i++) {
            yield return ObjectMaker.CustomerMaker.Generate();
        }
    }

    private static async Task<Customer> SetCustomer(CacheClient<Customer> client) {
        var value = ObjectMaker.CustomerMaker.Generate();
        var key = value.Id.ToString();
        var result = await client.SetAsync(key, value);
        return value;
    }

    private static async Task TestSetGet(CacheClient<string> client) {
        var value = "lorem ipsum dolor sit amet";
        var key = "random:key";
        var result = await client.SetAsync(key, value);
        var removeValue = await client.RemoveAsync(key);
        var getValue = await client.GetAsync(key);
    }

    private static async Task TestSetExpire(CacheClient<Customer> client) {
        var value = ObjectMaker.CustomerMaker.Generate();
        var key = value.Id.ToString();
        var result = await client.SetAsync(key, value, TimeSpan.FromSeconds(5));
        await Task.Delay(2000);
        var getValue = await client.GetAsync(key);
        await Task.Delay(6000);
        var nothing = await client.GetAsync(key);
    }

    private static async Task TestSetGetRemove(CacheClient<Customer> client) {
        var value = ObjectMaker.CustomerMaker.Generate();
        var key = value.Id.ToString();
        var result = await client.SetAsync(key, value);
        var getValue = await client.GetAsync(key);
        var nothing = await client.RemoveAsync(key);
    }
}
