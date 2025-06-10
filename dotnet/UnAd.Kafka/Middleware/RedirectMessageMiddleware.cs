namespace UnAd.Kafka.Middleware;

public sealed class RedirectMessageMiddleware<TKey, TValue>(KafkaMessageProducer<TKey, TValue> producer)
    : ErrorHandlingMiddleware<TKey, TValue> {

    protected override MessageContext<TKey, TValue> OnError(MessageContext<TKey, TValue> context, Exception ex) {
        try {
            producer.Produce(
                context.ConsumeResult.Message.Key,
                context.ConsumeResult.Message.Value,
                CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        } catch (Exception e) {
            // TODO: real logging
            Console.Error.WriteLine($"Failed to produce to DLQ: {e}");
        }

        return context with {
            Failed = true
        };
    }
}
