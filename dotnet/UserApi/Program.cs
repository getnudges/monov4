using Confluent.Kafka;
using HotChocolate.Diagnostics;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nudges.Auth;
using Nudges.Configuration.Extensions;
using Nudges.Data;
using Nudges.Data.Security;
using Nudges.Data.Users;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Security;
using Nudges.Telemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using UserApi;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

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

builder.Services.Configure<OidcConfig>(builder.Configuration.GetSection("Oidc"));
builder.Services.Configure<HashSettings>(builder.Configuration.GetSection("HashSettings"));
builder.Services.AddOptions<HashSettings>(nameof(HashSettings));
builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.AddOptions<EncryptionSettings>(nameof(EncryptionSettings));
builder.Services.AddSingleton(static sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var base64 = config["HashSettings:HashKeyBase64"]
        ?? throw new InvalidOperationException("Missing HashSettings:HashKeyBase64");

    var keyBytes = Convert.FromBase64String(base64);
    return new HashService(
        Options.Create(new HashSettings { HashKey = keyBytes })
    );
});
builder.Services.AddSingleton<IEncryptionService>(static sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var base64 = config["EncryptionSettings:Key"]
        ?? throw new InvalidOperationException("Missing EncryptionSettings:Key");

    var keyBytes = Convert.FromBase64String(base64);
    return new AesGcmEncryptionService(
        Options.Create(new EncryptionSettings { Key = keyBytes })
    );
});
builder.Services.AddSingleton<HashingSaveChangesInterceptor>();
builder.Services.AddSingleton<EncryptionSaveChangesInterceptor>();
builder.Services.AddSingleton<EncryptionMaterializationInterceptor>();

builder.Services.AddDbContextFactory<UserDbContext>(static (sp, o) => {
    o.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString(DbConstants.UserDb), static pg => {
        pg.EnableRetryOnFailure(3);
        pg.CommandTimeout(30);
    });
    o.EnableDetailedErrors();

    o.AddInterceptors(
        sp.GetRequiredService<EncryptionSaveChangesInterceptor>(),
        sp.GetRequiredService<EncryptionMaterializationInterceptor>(),
        sp.GetRequiredService<HashingSaveChangesInterceptor>()
    );
});

builder.Services
    .AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(sp =>
        new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
            BootstrapServers = settings.Kafka.BrokerList
        }))
    .AddSingleton<KafkaMessageProducer<ClientKey, ClientEvent>>(sp =>
        new ClientEventProducer(Topics.Clients, new ProducerConfig {
            BootstrapServers = settings.Kafka.BrokerList
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
    .AddInstrumentation(o => {
        o.Scopes = ActivityScopes.ResolveFieldValue;
        o.IncludeDocument = false;
        o.RequestDetails = RequestDetails.Default;
    })
    .InitializeOnStartup();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHealthChecks("/health");

if (settings.Otlp.Endpoint is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
