using GraphMonitor;
using Precision.WarpCache;
using Precision.WarpCache.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddExceptionHandler<Exception>((o, ex) =>
    o.ExceptionHandler = async context => {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(ex.Message);
    });

builder.Services.AddLogging(o => o.AddSimpleConsole(o => o.SingleLine = true));

builder.Services.AddSingleton<ICacheStore<string, string>, RedisCacheStore<string>>(
    s => new RedisCacheStore<string>(
        s.GetRequiredService<IConnectionMultiplexer>(),
        StringMessageSerializerContext.Default.String));

builder.Services.AddSingleton<IConnectionMultiplexer>(s =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("REDIS_URL")!));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/health")) {
        await next.Invoke(context);
        return;
    }
    if (context.Request.Headers.TryGetValue("X-Api-Key", out var value) &&
        value == context.RequestServices.GetRequiredService<IConfiguration>().GetValue<string>("MONITOR_API_KEY")) {
        await next.Invoke(context);
        return;
    }
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Unauthorized");
});

app.MapGet("/{name}", async (string name, ICacheStore<string, string> cache) => {

    var url = await cache.GetAsync($"graph:{name}");
    if (!url.Found) {
        return Results.NotFound();
    }
    return Results.Text(url.Value);
});

app.MapPost("/{name}", async (string name, HttpRequest request, ICacheStore<string, string> cache) => {
    using var reader = new StreamReader(request.Body);
    var url = await reader.ReadToEndAsync();
    if (string.IsNullOrEmpty(url)) {
        return Results.BadRequest();
    }

    await cache.SetAsync($"graph:{name}", url);

    var logger = request.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    GraphMonitorLogs.LogGraphStored(logger, name, null);
    return Results.Ok();
});

app.MapPost("/schema", async (HttpRequest request, ICacheStore<string, string> cache) => {
    using var reader = new StreamReader(request.Body);
    var schema = await reader.ReadToEndAsync();
    if (string.IsNullOrEmpty(schema)) {
        return Results.BadRequest();
    }

    await cache.SetAsync($"graph:schema", schema);

    var logger = request.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    GraphMonitorLogs.LogSchemaStored(logger, null);
    return Results.Ok();
});

app.MapGet("/schema", async (HttpRequest request, ICacheStore<string, string> cache) => {

    var value = await cache.GetAsync($"graph:schema");
    if (!value.Found) {
        return Results.NotFound();
    }
    return Results.Content(value.ToString(), "application/graphql+schema", System.Text.Encoding.UTF8);
});

await app.RunAsync();

internal class GraphMonitorLogs {
    public static readonly Action<ILogger<Program>, string, Exception?> LogGraphStored =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "GraphStored"), "Graph {Name} stored");
    public static readonly Action<ILogger<Program>, Exception?> LogSchemaStored =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "LogSchemaStored"), "Schema stored");
}

