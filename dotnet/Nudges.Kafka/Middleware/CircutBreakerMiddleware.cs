namespace Nudges.Kafka.Middleware;

public sealed class CircuitBreakerMiddleware<TKey, TValue>(
    TimeProvider timeProvider,
    int failureThreshold = 5,
    TimeSpan? openInterval = null,
    TimeSpan? rollingWindow = null) : IMessageMiddleware<TKey, TValue> {

    private readonly int _failureThreshold = failureThreshold;
    private readonly TimeSpan _openInterval = openInterval ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _rollingWindow = rollingWindow ?? TimeSpan.FromSeconds(10);

    private CircuitState _state = CircuitState.Closed;
    private DateTimeOffset _circuitOpenedTime;

    private readonly Queue<DateTimeOffset> _recentFailures = new();

    public async Task<MessageContext<TKey, TValue>> InvokeAsync(MessageContext<TKey, TValue> context, MessageHandler<TKey, TValue> next) {

        if (_state == CircuitState.Open) {
            if (timeProvider.GetUtcNow() - _circuitOpenedTime > _openInterval) {
                _state = CircuitState.HalfOpen;
            } else {
                return context with { Failed = true };
            }
        }

        try {
            var result = await next(context);

            if (_state == CircuitState.HalfOpen) {
                _state = CircuitState.Closed;
                _recentFailures.Clear();
            }

            return result;
        } catch {

            var now = timeProvider.GetUtcNow();
            _recentFailures.Enqueue(now);

            while (_recentFailures.Count > 0 && (now - _recentFailures.Peek()) > _rollingWindow) {
                _recentFailures.Dequeue();
            }

            if (_state == CircuitState.HalfOpen) {
                OpenCircuit();
                throw; // rethrow to let next layer handle it (maybe a DLQ)
            }

            if (_recentFailures.Count >= _failureThreshold) {
                OpenCircuit();
            }

            throw;
        }
    }

    private void OpenCircuit() {
        _state = CircuitState.Open;
        _circuitOpenedTime = timeProvider.GetUtcNow();
    }
    private enum CircuitState {
        Closed,
        Open,
        HalfOpen
    }

}
