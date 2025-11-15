using System.Text.RegularExpressions;
using GraphQLGateway;
using HotChocolate.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Precision.WarpCache.Grpc.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(o => o.SingleLine = true);

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource
                .AddService(builder.Environment.ApplicationName))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http"
                ]).AddPrometheusExporter())
        .WithTracing(traceConfig =>
            traceConfig
                .SetSampler<AlwaysOnSampler>()
                .AddAspNetCoreInstrumentation(options => {
                    options.Filter = context => context.Request.Method == "POST";
                    options.RecordException = true;
                    options.EnrichWithHttpResponse = (activity, response) =>
                        activity.DisplayName = response.HttpContext.Items["OperationName"]?.ToString() ?? "GraphQLRequest";
                }))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryMeterProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets(typeof(Program).Assembly);

builder.Services.AddHeaderPropagation(static o => {
    o.Headers.Add("traceparent");
    o.Headers.Add("tracestate");

    o.Headers.Add("Authorization", static c => {
        if (c.HttpContext.Items.TryGetValue("cachedJwt", out var cachedJwt) && cachedJwt is string jwt) {
            return $"Bearer {jwt}";
        }
        return c.HeaderValue;
    });

    o.Headers.Add("Cookie", "Authorization", static c => {
        if (c.HttpContext.Items.TryGetValue("cachedJwt", out var cachedJwt) && cachedJwt is string jwt) {
            return $"Bearer {jwt}";
        }
        return default;
    });
});

builder.Services.AddHttpClient("Fusion")
    .AddDefaultLogger()
    .AddHeaderPropagation()
    .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());

builder.Services.AddWarpCacheClient(
    builder.Configuration.GetValue<string>("CACHE_SERVER_ADDRESS")!,
    StringMessageSerializerContext.Default.String);

builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromFile("./gateway.fgp")
    .ModifyFusionOptions(opt => {
        opt.AllowQueryPlan = builder.Environment.IsDevelopment();
        opt.IncludeDebugInfo = builder.Environment.IsDevelopment();
    })
    .UseDefaultPipeline()
    .CoreBuilder
    .ModifyRequestOptions(opt =>
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.Use(async (context, next) => {
    var cache = context.RequestServices.GetRequiredService<ICacheClient<string>>();
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
        var tokenId = authHeader.ToString().Split(' ').LastOrDefault();
        if (!string.IsNullOrWhiteSpace(tokenId)) {
            if (tokenId.IndexOf('.', StringComparison.OrdinalIgnoreCase) > 0) {
                await next();
                return;
            }
            var jwt = await cache.GetAsync($"token:{tokenId}", context.RequestAborted);
            if (jwt is not null) {
                context.Items["cachedJwt"] = jwt;
            }
        }
    }

    if (context.Request.Cookies.TryGetValue("TokenId", out var tokenIdFromCookie) &&
        !string.IsNullOrWhiteSpace(tokenIdFromCookie)) {
        var jwt = await cache.GetAsync($"token:{tokenIdFromCookie}", context.RequestAborted);
        if (jwt is not null) {
            context.Items["cachedJwt"] = jwt;
        }
    }

    await next();
});

app.MapHealthChecks("/health");

app.UseHeaderPropagation();

app.UseWebSockets();

app.Use(async (context, next) => {
    if (!context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/graphql"))) {
        await next();
        return;
    }

    var activityName = await GetTraceActivityName(context.Request, context.RequestAborted);

    await next();

    context.Items["OperationName"] = activityName;
});

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.MapGraphQL()
    .WithOptions(new GraphQLServerOptions {
        Tool = { Enable = app.Environment.IsDevelopment() },
        EnableSchemaRequests = app.Environment.IsDevelopment(),
        EnableGetRequests = false,
    });

app.Logger.LogAppStarting();

app.RunWithGraphQLCommands(args);

static async Task<string?> GetTraceActivityName(HttpRequest request, CancellationToken cancellationToken) {
    try {
        request.EnableBuffering();
        using var reader = new StreamReader(
            request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var requestBody = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;

        var matches = MutationNameRegex.Matches(requestBody);
        return matches.Count > 0 ? matches[0].Groups[2].Value : "GraphQLRequest";
    } catch {
        return "GraphQLRequest";
    }
}

public partial class Program {
    private static readonly Regex MutationNameRegex = MutationNameRegexDef();

    [GeneratedRegex(@"(mutation)\s+(\w+)\(", RegexOptions.Compiled)]
    private static partial Regex MutationNameRegexDef();
}
