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

// -----------------------------------------------------------------------------
// OpenTelemetry: Traces + Metrics + Logs
// -----------------------------------------------------------------------------
builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets(typeof(Program).Assembly);

if (builder.Configuration.GetValue<string>("Otlp__Endpoint") is string url)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource
                .AddService(builder.Environment.ApplicationName)
                .AddAttributes(new Dictionary<string, object> {
                    ["service.version"] = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                    ["service.namespace"] = "Nudges",
                }))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http"
                ])
                .AddPrometheusExporter())
        .WithTracing(traceConfig =>
            traceConfig
                .SetSampler<AlwaysOnSampler>()
                // HTTP server spans for /graphql
                .AddAspNetCoreInstrumentation(options => {
                    options.Filter = context => context.Request.Method == "POST";
                    options.RecordException = true;
                    options.EnrichWithHttpResponse = (activity, response) => {
                        // Use parsed operation name (if available) as display name
                        var httpContext = response.HttpContext;

                        var opName = httpContext.Items.TryGetValue("OperationName", out var nameObj)
                            ? nameObj?.ToString()
                            : null;

                        var opType = httpContext.Items.TryGetValue("GraphQLOperationType", out var typeObj)
                            ? typeObj?.ToString()
                            : null;

                        activity.DisplayName = !string.IsNullOrWhiteSpace(opName)
                            ? opName!
                            : "GraphQLRequest";

                        if (!string.IsNullOrWhiteSpace(opName)) {
                            activity.SetTag("graphql.operation.name", opName);
                        }

                        if (!string.IsNullOrWhiteSpace(opType)) {
                            activity.SetTag("graphql.operation.type", opType);
                        }

                        // This span is effectively a GraphQL HTTP gateway call
                        activity.SetTag("graphql.transport", "http");
                        activity.SetTag("url.path", response.HttpContext.Request.Path.ToString());
                    };
                })

                // HotChocolate-level instrumentation (execution, resolvers, etc.)
                .AddHotChocolateInstrumentation()
                // Outgoing HTTP to downstream services
                .AddHttpClientInstrumentation(o => {
                    o.RecordException = true;
                }))
        .WithLogging();

    // Exporters
    builder.Services.ConfigureOpenTelemetryMeterProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

// -----------------------------------------------------------------------------
// Header propagation: tracing + auth + baggage
// -----------------------------------------------------------------------------
builder.Services.AddHeaderPropagation(static o => {
    // Trace context
    o.Headers.Add("traceparent");
    o.Headers.Add("tracestate");

    // Baggage for cross-service correlation (user id, tenant id, etc.)
    o.Headers.Add("baggage");

    // Auth: propagate JWT (possibly looked up from WarpCache)
    o.Headers.Add("Authorization", static c => {
        if (c.HttpContext.Items.TryGetValue("cachedJwt", out var cachedJwt) && cachedJwt is string jwt) {
            return $"Bearer {jwt}";
        }
        return c.HeaderValue;
    });

    // Cookie-based token -> Authorization header
    o.Headers.Add("Cookie", "Authorization", static c => {
        if (c.HttpContext.Items.TryGetValue("cachedJwt", out var cachedJwt) && cachedJwt is string jwt) {
            return $"Bearer {jwt}";
        }
        return default;
    });
});

// -----------------------------------------------------------------------------
// Downstream HTTP client for Fusion gateway
// -----------------------------------------------------------------------------
builder.Services.AddHttpClient("Fusion")
    .AddDefaultLogger()
    .AddHeaderPropagation()
    .AddPolicyHandler(PollyPolicies.GetResiliencePolicy());

// -----------------------------------------------------------------------------
// WarpCache client
// -----------------------------------------------------------------------------
builder.Services.AddWarpCacheClient(
    builder.Configuration.GetValue<string>("WarpCache__Url")!,
    StringMessageSerializerContext.Default.String);

// -----------------------------------------------------------------------------
// GraphQL Fusion Gateway
// -----------------------------------------------------------------------------
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
    .AddInstrumentation(o => {
        o.Scopes = HotChocolate.Diagnostics.ActivityScopes.None;
        o.IncludeDocument = false;
        o.RenameRootActivity = true;
        o.RequestDetails = HotChocolate.Diagnostics.RequestDetails.Default;
    })
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

// -----------------------------------------------------------------------------
// Middleware: JWT resolution via WarpCache
// -----------------------------------------------------------------------------
app.Use(async (context, next) => {
    var cache = context.RequestServices.GetRequiredService<ICacheClient<string>>();

    // Authorization: Bearer <tokenId or jwt>
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
        var tokenId = authHeader.ToString().Split(' ').LastOrDefault();
        if (!string.IsNullOrWhiteSpace(tokenId)) {
            // If it's already a JWT (contains '.'), bypass lookup
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

    // Cookie: TokenId -> JWT
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

// -----------------------------------------------------------------------------
// Middleware: derive GraphQL operation name + type for span enrichment
// -----------------------------------------------------------------------------
app.Use(async (context, next) => {
    if (!context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/graphql"))) {
        await next();
        return;
    }

    var (activityName, operationType) = await GetTraceActivity(context.Request, context.RequestAborted);

    // Stash for the ASP.NET Core + HotChocolate enrichers
    // I only care if we found a proper operation name
    if (!string.IsNullOrWhiteSpace(operationType)) {
        context.Items["OperationName"] = activityName;
        context.Items["GraphQLOperationType"] = operationType;
    }

    await next();
});

if (builder.Configuration.GetValue<string>("Otlp__Endpoint") is not null)
{
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

// -----------------------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------------------
static async Task<(string Name, string? OperationType)> GetTraceActivity(HttpRequest request, CancellationToken cancellationToken) {
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
        if (matches.Count > 0) {
            var opType = matches[0].Groups[1].Value; // query | mutation | subscription
            var opName = matches[0].Groups[2].Value; // operation name
            return (opName, opType);
        }

        return ("GraphQLRequest(Unknown)", null);
    } catch (Exception e) {
        return ($"GraphQLRequestError({e.GetType().Name})", null);
    }
}

// Needed for WebApplicationFactory, tests, etc.
public partial class Program {
    // Capture query | mutation | subscription operation + name
    private static readonly Regex MutationNameRegex = MutationNameRegexDef();

    [GeneratedRegex(@"\b(query|mutation|subscription)\s+(\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex MutationNameRegexDef();
}
