using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ProductApi.Tests.Integration;

public class ApiFactory
    : WebApplicationFactory<Program>, IAsyncLifetime {
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureTestServices(services => {
            services.Remove(services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<UserDbContext>))!);
            services.Remove(services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbConnection))!);
            services.AddPooledDbContextFactory<UserDbContext>(o
                => o.UseNpgsql(_postgresContainer.GetConnectionString()));

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

            services.AddSingleton(sp =>
                new RequestExecutorProxy(sp.GetRequiredService<IRequestExecutorResolver>(), Schema.DefaultName));
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync() {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync() {
        await _postgresContainer.DisposeAsync()
            .AsTask();
        await _redisContainer.DisposeAsync()
            .AsTask();
        await base.DisposeAsync();
    }

    public async Task<QueryResult> ExecuteRequestAsync(Action<IQueryRequestBuilder> configRequest,
        CancellationToken cancellationToken = default) {
        await using var scope = Services.CreateAsyncScope();
        var requestBuilder = new QueryRequestBuilder();
        requestBuilder.SetServices(scope.ServiceProvider);
        configRequest(requestBuilder);
        var request = requestBuilder.Create();
        var result = await scope.ServiceProvider.GetRequiredService<RequestExecutorProxy>()
            .ExecuteAsync(request, cancellationToken);
        result.RegisterForCleanup(scope.DisposeAsync);
        result.ExpectQueryResult();
        return result as QueryResult ?? throw new InvalidOperationException("The result is not a query result.");
    }
}



