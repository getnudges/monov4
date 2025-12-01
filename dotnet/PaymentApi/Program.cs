using Confluent.Kafka;
using HotChocolate.Diagnostics;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Data;
using Nudges.Data.Payments;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Telemetry;
using OpenTelemetry.Trace;
using PaymentApi;
using PaymentApi.Services;
using StackExchange.Redis;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

builder.Services.AddHttpContextAccessor();

if (settings.Otlp.Endpoint is string url && !string.IsNullOrEmpty(url)) {

    builder.Services.AddOpenTelemetryConfiguration<Program>(
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
        ], null, o => o.AddHotChocolateInstrumentation(), options => {
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

builder.Services.AddTransient<IConnectionMultiplexer>(static c =>
    ConnectionMultiplexer.Connect(c.GetRequiredService<IConfiguration>().GetRedisUrl()));

builder.Services.AddPooledDbContextFactory<PaymentDbContext>(static (s, o) =>
    o.UseNpgsql(s.GetRequiredService<IConfiguration>().GetConnectionString(DbConstants.PaymentDb)));

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
        BootstrapServers = settings.Kafka.BrokerList
    }));

builder.Services.AddSingleton<KafkaMessageProducer<PaymentKey, PaymentEvent>>(sp =>
    new PaymentEventProducer(Topics.Payments, new ProducerConfig {
        BootstrapServers = settings.Kafka.BrokerList
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
    .AddInstrumentation(o => {
        o.Scopes = ActivityScopes.ResolveFieldValue;
        o.IncludeDocument = false;
        o.RequestDetails = RequestDetails.Default;
    })
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

if (settings.Otlp.Endpoint is string e && !string.IsNullOrEmpty(e)) {
    app.MapPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
