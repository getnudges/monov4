namespace Nudges.Kafka.Middleware;

public abstract class ErrorHandlingMiddleware<TKey, TValue> : IMessageMiddleware<TKey, TValue> {

    public virtual async Task<MessageContext<TKey, TValue>> InvokeAsync(MessageContext<TKey, TValue> context, MessageHandler<TKey, TValue> next) {
        try {
            return await next(context);
        } catch (Exception ex) {
            return OnError(context with {
                Failed = true
            }, ex);
        }
    }

    protected abstract MessageContext<TKey, TValue> OnError(MessageContext<TKey, TValue> context, Exception ex);
}
