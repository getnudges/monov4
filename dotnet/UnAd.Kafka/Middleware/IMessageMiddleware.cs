namespace UnAd.Kafka.Middleware;

public interface IMessageMiddleware<TKey, TValue> {
    public Task<MessageContext<TKey, TValue>> InvokeAsync(MessageContext<TKey, TValue> context, MessageHandler<TKey, TValue> next);
}
