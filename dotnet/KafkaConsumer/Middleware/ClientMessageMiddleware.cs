using Confluent.Kafka;
using KafkaConsumer.Services;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

internal class ClientMessageMiddleware(ILogger<ClientMessageMiddleware> logger,
                                       Func<INudgesClient> nudgesClientFactory,
                                       IForeignProductService foreignProductService,
                                       KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<ClientKey, ClientEvent> {
    public async Task<MessageContext<ClientKey, ClientEvent>> InvokeAsync(MessageContext<ClientKey, ClientEvent> context, MessageHandler<ClientKey, ClientEvent> next) {
        var result = await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        result.Match(
            _ => logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully."),
            err => logger.LogMessageUhandled(context.ConsumeResult.Message.Key.ToString(), err)); // TODO: handle errors better (maybe retry?)
        return context;
    }

    public async Task<Result<bool, Exception>> HandleMessageAsync(ConsumeResult<ClientKey, ClientEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");
        return await (cr.Message.Value switch {
            ClientCreatedEvent created => HandleClientCreated(created.ClientNodeId, cancellationToken),
            ClientUpdatedEvent updated => HandleClientUpdated(updated.ClientNodeId, cancellationToken),
            _ => Result.ExceptionTask(new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.")),
        });
    }

    private async Task<Result<bool, Exception>> HandleClientCreated(string clientNodeId, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        return await client.Instance.GetClient(clientNodeId, cancellationToken).Map(async clientInfo => {
            return await foreignProductService.CreateCustomer(
                clientNodeId, clientInfo.PhoneNumber, clientInfo.Name, cancellationToken).Map(async customerId =>
                    await client.Instance.UpdateClient(new UpdateClientInput {
                        Id = clientNodeId,
                        CustomerId = customerId,
                    }, cancellationToken)).Map(async _ =>
                        // TODO: include link to the client to set up their Keycloak-based account
                        await SendSms(clientInfo.PhoneNumber, "ClientCreated", clientInfo.Locale, [], cancellationToken));
        });
    }

    private async Task<Result<bool, Exception>> SendSms(string phoneNumber,
                                                        string resourceKey,
                                                        string locale,
                                                        Dictionary<string, string> replacements,
                                                        CancellationToken cancellationToken) {
        try {
            var result = await notificationProducer.ProduceSendSms(phoneNumber, resourceKey, locale, replacements, cancellationToken);
            return result.Status == PersistenceStatus.Persisted;
        } catch (Exception e) {
            return e;
        }
    }

    private Task<Result<bool, Exception>> HandleClientUpdated(string clientId, CancellationToken cancellationToken) {
        //using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);
        //var result = await client.Instance.GetClient.ExecuteAsync(clientId, cancellationToken);

        // TODO: send off a notification about the updated data
        return Result.SuccessTask<bool, Exception>(true);
    }
}
