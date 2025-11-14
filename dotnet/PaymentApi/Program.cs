using Confluent.Kafka;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Data.Payments;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PaymentApi;
using PaymentApi.Services;
using StackExchange.Redis;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

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
                    $"{typeof(Mutation).FullName}",
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                .AddSource([
                    $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer",
                    $"{typeof(StripePaymentProvider).FullName}",
                    $"{typeof(Mutation).FullName}",
                ]))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Services.AddTransient<IConnectionMultiplexer>(static c =>
    ConnectionMultiplexer.Connect(c.GetRequiredService<IConfiguration>().GetRedisUrl()));

builder.Services.AddPooledDbContextFactory<PaymentDbContext>(static (s, o) =>
    o.UseNpgsql(s.GetRequiredService<IConfiguration>().GetConnectionString("PaymentDb")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));

builder.Services.AddSingleton<KafkaMessageProducer<PaymentKey, PaymentEvent>>(static sp =>
    new PaymentEventProducer(Topics.Payments, new ProducerConfig {
        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
    }));

builder.Services.AddTransient<IStripeClient>(static s => {
    var config = s.GetRequiredService<IConfiguration>();
    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
});

builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();



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
            //Console.WriteLine("Token validated: {0}", string.Join(',', context.Principal?.Claims.Select(c => c.Value) ?? []));
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context => {
            Console.WriteLine("Authentication failed: {0}", context.Exception.Message);
            Console.WriteLine("Authority: {0}", context.Options.Authority);
            return Task.CompletedTask;
        }
    };
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
    .AddPaymentApiTypes()
    .AddDiagnosticEventListener<LoggerExecutionEventListener>()
    .RegisterDbContextFactory<PaymentDbContext>()
    .ModifyRequestOptions(opt =>
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    //.AddRedisSubscriptions(static p => p.GetRequiredService<IConnectionMultiplexer>())
    .ModifyOptions(static o => o.ValidatePipelineOrder = false)
    //.AddQueryFieldToMutationPayloads()
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
