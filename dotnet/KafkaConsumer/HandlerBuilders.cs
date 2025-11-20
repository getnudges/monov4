using Confluent.Kafka;
using KafkaConsumer;
using KafkaConsumer.Middleware;
using KafkaConsumer.Notifications;
using KafkaConsumer.Services;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nudges.Configuration.Extensions;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Stripe;
using Twilio;
using Twilio.Clients;

internal static class HandlerBuilders {
    public static IHostBuilder ConfigureNotificationHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                if (hostContext.HostingEnvironment.IsDevelopment()) {
                    services.AddSingleton<INotifier, LocalNotifier>();
                } else {
                    services.AddSingleton<Func<ITwilioRestClient>>(static sp => {
                        var config = sp.GetRequiredService<IConfiguration>();
                        TwilioClient.Init(config.GetTwilioAccountSid(),
                           config.GetTwilioAuthToken());
                        return TwilioClient.GetRestClient;
                    });
                    services.AddSingleton<INotifier, TwilioNotifier>();
                }

                services.AddTransient<NotificationMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<NotificationKey, NotificationEvent>(
                            Topics.Notifications,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<NotificationKey, NotificationEvent>())
                        .Use(sp.GetRequiredService<NotificationMessageMiddleware>())
                        .Build());

                services.AddHostedService<MessageHandlerService<NotificationKey, NotificationEvent>>();
            });
    public static IHostBuilder ConfigurePlanSubscriptionHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));
                services.AddSingleton<KafkaMessageProducer<ClientKey, ClientEvent>>(static sp =>
                    new ClientEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>, PlanSubscriptionEventMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PlanSubscriptionKey, PlanSubscriptionEvent>(
                            Topics.PlanSubscriptions,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PlanSubscriptionKey, PlanSubscriptionEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PlanSubscriptionKey, PlanSubscriptionEvent>>();
            });
    public static IHostBuilder ConfigureClientHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddKeycloakAdminHttpClient(hostContext.Configuration);

                services.AddTransient<IMessageMiddleware<ClientKey, ClientEvent>, ClientMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<ClientKey, ClientEvent>(
                            Topics.Clients,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<ClientKey, ClientEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<ClientKey, ClientEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<ClientKey, ClientEvent>>();
            });
    public static IHostBuilder ConfigureUserAuthenticationHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));
                services.AddSingleton<KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent>>(static sp =>
                    new DeadLetterEventProducer(Topics.DeadLetter, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>, UserAuthenticationMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<UserAuthenticationEventKey, UserAuthenticationEvent>(
                            Topics.UserAuthentication,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>())
                        .Use(new SmartErrorHandlingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>(
                            sp.GetRequiredService<ILogger<SmartErrorHandlingMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>>>(),
                            sp.GetRequiredService<KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent>>()
                        ))
                        .Use(sp.GetRequiredService<IMessageMiddleware<UserAuthenticationEventKey, UserAuthenticationEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<UserAuthenticationEventKey, UserAuthenticationEvent>>();
            });
    public static IHostBuilder ConfigurePaymentEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton<KafkaMessageProducer<NotificationKey, NotificationEvent>>(static sp =>
                    new NotificationEventProducer(Topics.Notifications, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IMessageMiddleware<PaymentKey, PaymentEvent>, PaymentMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PaymentKey, PaymentEvent>(
                            Topics.Payments,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PaymentKey, PaymentEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PaymentKey, PaymentEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PaymentKey, PaymentEvent>>();
            });

    public static IHostBuilder ConfigurePlanEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddSingleton(static sp =>
                    new PriceTierEventProducer(Topics.PriceTiers, new ProducerConfig {
                        BootstrapServers = sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                        AllowAutoCreateTopics = true,
                    }));

                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PlanEventKey, PlanChangeEvent>, PlanMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PlanEventKey, PlanChangeEvent>(
                            Topics.Plans,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PlanEventKey, PlanChangeEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PlanEventKey, PlanChangeEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PlanEventKey, PlanChangeEvent>>();
            });

    public static IHostBuilder ConfigureForeignProductEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddTransient<IMessageMiddleware<ForeignProductEventKey, ForeignProductEvent>, ForeignProductMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<ForeignProductEventKey, ForeignProductEvent>(
                            Topics.Plans,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<ForeignProductEventKey, ForeignProductEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<ForeignProductEventKey, ForeignProductEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<ForeignProductEventKey, ForeignProductEvent>>();
            });

    public static IHostBuilder ConfigurePriceTierEventHandler(this IHostBuilder builder) =>
        builder
            .ConfigureServices(static (hostContext, services) => {
                services.AddTransient<IStripeClient>(s => {
                    var config = s.GetRequiredService<IConfiguration>();
                    return new StripeClient(apiKey: config.GetStripeApiKey(), apiBase: config.GetStripeApiUrl());
                });
                services.AddTransient<IForeignProductService, StripeService>();

                services.AddTransient<IMessageMiddleware<PriceTierEventKey, PriceTierEvent>, PriceTierMessageMiddleware>();
                services.AddTransient(static sp =>
                    KafkaMessageProcessorBuilder
                        .For<PriceTierEventKey, PriceTierEvent>(
                            Topics.PriceTiers,
                            sp.GetRequiredService<IConfiguration>().GetKafkaBrokerList(),
                            cancellationToken: sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping)
                        .Use(new TracingMiddleware<PriceTierEventKey, PriceTierEvent>())
                        .Use(sp.GetRequiredService<IMessageMiddleware<PriceTierEventKey, PriceTierEvent>>())
                        .Build());

                services.AddHostedService<MessageHandlerService<PriceTierEventKey, PriceTierEvent>>();
            });
}
