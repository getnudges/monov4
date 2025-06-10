using Confluent.Kafka;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using UnAd.Auth;
using UnAd.Configuration.Extensions;
using UnAd.Data.Users;
using UnAd.Kafka;
using UserApi;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetOltpEndpointUrl() is string url) {

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
                ]).AddPrometheusExporter())
        .WithTracing(traceBuilder =>
            traceBuilder
                .SetSampler<AlwaysOnSampler>()
                //.AddEntityFrameworkCoreInstrumentation()
                .AddSource([
                    $"{typeof(KafkaMessageProducer<,>).Namespace}.KafkaMessageProducer"
                ]))
        .WithLogging();

    builder.Services.ConfigureOpenTelemetryTracerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));

    builder.Services.ConfigureOpenTelemetryLoggerProvider(o =>
        o.AddOtlpExporter(o => o.Endpoint = new Uri(url)));
}

builder.Services.AddTransient<IConnectionMultiplexer>(c =>
    ConnectionMultiplexer.Connect(c.GetRequiredService<IConfiguration>().GetRedisUrl()));

builder.Services.AddPooledDbContextFactory<UserDbContext>((s, o) =>
    o.UseNpgsql(s.GetRequiredService<IConfiguration>().GetConnectionString(UnAd.Data.DbConstants.UserDb)));

builder.Services
    .AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
        new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }))
    .AddSingleton<KafkaMessageProducer<ClientKey, ClientEvent>>(sp =>
        new ClientEventProducer(Topics.Clients, new ProducerConfig {
            BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList()
        }));

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
    .RegisterDbContextFactory<UserDbContext>()
    .AddUserApiTypes()
    .AddDiagnosticEventListener<LoggerExecutionEventListener>()
    .AddFiltering()
    .AddPagingArguments()
    .AddProjections()
    .AddSorting()
    .AddMutationConventions()
    .AddGlobalObjectIdentification()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
    .AddRedisSubscriptions((sp) => sp.GetRequiredService<IConnectionMultiplexer>())
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

if (builder.Configuration.GetOltpEndpointUrl() is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);

public partial class Program { }



