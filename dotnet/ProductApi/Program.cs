using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductApi;
using StackExchange.Redis;
using Nudges.Auth;
using Nudges.Auth.Web;
using Nudges.Data;
using Nudges.Data.Products;
using Nudges.Kafka;
using Nudges.Configuration.Extensions;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(static o => o.SingleLine = true);

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is string url) {

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(builder.Environment.ApplicationName))
        .WithMetrics(o =>
            o.AddRuntimeInstrumentation()
                .AddMeter([
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http",
                    //"Microsoft.EntityFrameworkCore",
                    $"{typeof(Mutation).FullName}"
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                //.AddEntityFrameworkCoreInstrumentation()
                .AddSource([
                    $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
                    $"{typeof(Mutation).FullName}"
                ]))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryMeterProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
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
    .AddSingleton<KafkaMessageProducer<PlanKey, PlanEvent>>(sp =>
        new PlanEventProducer(Topics.Plans, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<PriceTierEventKey, PriceTierEvent>>(sp =>
        new PriceTierEventProducer(Topics.PriceTiers, new ProducerConfig {
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


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration, options => {
    var config = builder.Configuration.GetSection("Authentication:Schemes:Bearer").Get<JwtBearerOptions>();

    if (config is not null) {
        options.Authority = config.Authority ?? options.Authority;
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
        options.TokenValidationParameters = config.TokenValidationParameters ?? options.TokenValidationParameters;
    }
});


builder.Services.AddAuthorizationBuilder()
    //.AddDefaultPolicy("default", builder => builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser())
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
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

if (builder.Configuration.GetValue<string>("OTLP_ENDPOINT_URL") is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);

public partial class Program { }
