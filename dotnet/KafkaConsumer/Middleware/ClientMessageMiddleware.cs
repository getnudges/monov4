using Confluent.Kafka;
using KafkaConsumer.Services;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;
using Nudges.Security;

namespace KafkaConsumer.Middleware;

internal class ClientMessageMiddleware(ILogger<ClientMessageMiddleware> logger,
                                       Func<INudgesClient> nudgesClientFactory,
                                       IForeignProductService foreignProductService,
                                       IEncryptionService encryptionService,
                                       KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : IMessageMiddleware<ClientKey, ClientEvent> {
    public async Task<MessageContext<ClientKey, ClientEvent>> InvokeAsync(MessageContext<ClientKey, ClientEvent> context, MessageHandler<ClientKey, ClientEvent> next) {
        await HandleMessageAsync(context.ConsumeResult, context.CancellationToken);
        logger.LogAction($"Message {context.ConsumeResult.Message.Key} handled successfully.");
        return context with { Failure = FailureType.None };
    }

    public async Task HandleMessageAsync(ConsumeResult<ClientKey, ClientEvent> cr, CancellationToken cancellationToken) {
        logger.LogAction($"Received message {cr.Message.Key}");

        switch (cr.Message.Value) {
            case ClientCreatedEvent created:
                await HandleClientCreated(created, cancellationToken);
                break;
            case ClientUpdatedEvent updated:
                await HandleClientUpdated(updated, cancellationToken);
                break;
            default:
                throw new UnhandledMessageException($"No handler registered for event {cr.Message.Key}.");
        }
    }

    private async Task HandleClientCreated(ClientCreatedEvent data, CancellationToken cancellationToken) {
        using var client = new DisposableWrapper<INudgesClient>(nudgesClientFactory);

        var phoneNumber = encryptionService.Decrypt(data.PhoneNumberEncrypted);
        if (string.IsNullOrEmpty(phoneNumber)) {
            throw new Exception("Decrypted phone number is null or empty");
        }
        var customerId = await foreignProductService.CreateCustomer(data.UserId, phoneNumber, data.Name, cancellationToken);

        await client.Instance.UpdateClient(new UpdateClientInput {
            Id = data.UserId,
            CustomerId = customerId,
        }, cancellationToken);

        var result = await notificationProducer.ProduceSendClientCreated(data.PhoneNumberEncrypted, data.Locale, cancellationToken);
        if (result.Status != PersistenceStatus.Persisted) {
            throw new Exception($"Failed to send SMS to {data.UserId}");
        }
    }

    private Task HandleClientUpdated(ClientUpdatedEvent data, CancellationToken cancellationToken) {
        // TODO: send off a notification about the updated data
        return Task.CompletedTask;
    }
}
