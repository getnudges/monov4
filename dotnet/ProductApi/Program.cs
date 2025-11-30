using System.Text.RegularExpressions;
using Confluent.Kafka;
using HotChocolate.Diagnostics;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Data;
using Nudges.Data.Products;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Telemetry;
using OpenTelemetry.Trace;
using ProductApi;
using ProductApi.Telemetry;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(static o => o.SingleLine = true);

builder.Configuration.AddEnvironmentVariables().AddUserSecrets<Program>(optional: true);

if (builder.Configuration.GetOtlpEndpointUrl() is string url) {

    builder.Services.AddOpenTelemetryConfiguration(
        url,
        builder.Environment.ApplicationName, [
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http",
            "Microsoft.EntityFrameworkCore",
            $"{typeof(Mutation).FullName}"
        ], [
            $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
            $"{typeof(Mutation).FullName}"
        ], null, o => {
            o.AddHotChocolateInstrumentation();
        }, options => {
            options.RecordException = true;
            options.Filter = ctx => ctx.Request.Method == "POST";

            options.EnrichWithHttpResponse = (activity, response) => {
                if (response.HttpContext.Items.TryGetValue("OperationName", out var opNameObj) &&
                    opNameObj is string opName &&
                    !string.IsNullOrWhiteSpace(opName)) {
                    activity.DisplayName = opName;
                    activity.SetTag("graphql.operation.name", opName);
                }

                if (response.HttpContext.Items.TryGetValue("GraphQLOperationType", out var opTypeObj) &&
                    opTypeObj is string opType) {
                    activity.SetTag("graphql.operation.type", opType);
                }

                activity.SetTag("graphql.transport", "http");
            };
        });
}

builder.Services.AddTransient<IConnectionMultiplexer>(c =>
    ConnectionMultiplexer.Connect(c.GetRequiredService<IConfiguration>().GetRedisUrl()));

builder.Services.AddPooledDbContextFactory<ProductDbContext>((s, o) =>
    o.UseNpgsql(s.GetRequiredService<IConfiguration>().GetConnectionString(DbConstants.ProductDb)));

builder.Services
    .AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
        new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<PlanEventKey, PlanChangeEvent>>(sp =>
        new PlanChangeEventProducer(Topics.Plans, new ProducerConfig {
           BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<PriceTierEventKey, PriceTierChangeEvent>>(sp =>
        new PriceTierChangeEventProducer(Topics.PriceTiers, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<PlanSubscriptionKey, PlanSubscriptionEvent>>(sp =>
        new PlanSubscriptionEventProducer(Topics.PlanSubscriptions, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<DiscountCodeKey, DiscountCodeEvent>>(sp =>
        new DiscountCodeEventProducer(Topics.DiscountCodes, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.Admin, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Admin, ClaimValues.Roles.Service])))
    .AddPolicy(PolicyNames.Subscriber, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Subscriber])))
    .AddPolicy(PolicyNames.Client, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Client])));

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<ITracePropagator, TracePropagator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration, options => {
    var config = builder.Configuration.GetSection("Authentication:Schemes:Bearer").Get<JwtBearerOptions>();

    if (config is not null) {
        options.Authority = config.Authority ?? options.Authority;
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
        options.TokenValidationParameters = config.TokenValidationParameters ?? options.TokenValidationParameters;
    }
    if (builder.Environment.IsDevelopment()) {
        options.BackchannelHttpHandler = new HttpClientHandler {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }
    options.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            if (context.Request.Method == "POST") {
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context => {
            //if (context.Principal?.HasRole(ClaimValues.Roles.Service) == true) {
            //    Console.WriteLine("This is a service account");
            //}
            //Console.WriteLine("Token validated: {0}", string.Join(',', context.Principal?.Claims.Select(c => c.Value) ?? []));
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context => {
            //Console.WriteLine("Authentication failed: {0}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHeaderPropagation(o => {
    o.Headers.Add("traceparent");
    o.Headers.Add("tracestate");
    o.Headers.Add("baggage");
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.Admin, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Admin, ClaimValues.Roles.Service])))
    .AddPolicy(PolicyNames.Subscriber, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Subscriber])))
    .AddPolicy(PolicyNames.Client, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new RolesAuthorizationRequirement([ClaimValues.Roles.Client])));

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddFiltering()
    .AddProjections()
    .AddSorting()
    .AddMutationConventions()
    .AddGlobalObjectIdentification()
    .AddQueryType<QueryObjectType>()
    .AddMutationType<MutationObjectType>()
    .AddSubscriptionType<SubscriptionObjectType>()
    .AddDiagnosticEventListener<LoggerExecutionEventListener>()
    .RegisterDbContextFactory<ProductDbContext>()
    .ModifyRequestOptions(opt =>
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddRedisSubscriptions(p => p.GetRequiredService<IConnectionMultiplexer>())
    .AddInstrumentation(o => {
        o.Scopes = ActivityScopes.ResolveFieldValue;
        o.IncludeDocument = false;
        o.RequestDetails = RequestDetails.Default;
    })
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

if (builder.Configuration.GetValue<string>("Otlp__Endpoint") is not null)
{
    app.MapPrometheusScrapingEndpoint();
}

app.Use(async (context, next) => {
    if (!context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/graphql"))) {
        await next();
        return;
    }

    var (activityName, operationType) = await GetTraceActivity(context.Request, context.RequestAborted);

    // Stash for the ASP.NET Core + HotChocolate enrichers
    context.Items["OperationName"] = activityName;
    if (!string.IsNullOrWhiteSpace(operationType)) {
        context.Items["GraphQLOperationType"] = operationType;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapGraphQL();

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
    } catch {
        return ("GraphQLRequest(Err)", null);
    }
}

// Needed for WebApplicationFactory, tests, etc.
public partial class Program {
    // Capture query | mutation | subscription operation + name
    private static readonly Regex MutationNameRegex = MutationNameRegexDef();

    [GeneratedRegex(@"\b(query|mutation|subscription)\s+(\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex MutationNameRegexDef();
}
