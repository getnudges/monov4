using System.Diagnostics;

namespace Nudges.Kafka.Middleware;

public sealed class CircuitBreakerMiddleware<TKey, TValue>(
    TimeProvider timeProvider,
    int failureThreshold = 5,
    TimeSpan? openInterval = null,
    TimeSpan? rollingWindow = null)
    : IMessageMiddleware<TKey, TValue> {
    private readonly int _failureThreshold = failureThreshold;
    private readonly TimeSpan _openInterval = openInterval ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _rollingWindow = rollingWindow ?? TimeSpan.FromSeconds(10);

    private CircuitState _state = CircuitState.Closed;
    private DateTimeOffset _circuitOpenedTime;

    private readonly Queue<DateTimeOffset> _recentFailures = new();

    private static readonly ActivitySource ActivitySource =
        new($"{typeof(CircuitBreakerMiddleware<,>).Namespace}.CircuitBreaker");

    public async Task<MessageContext<TKey, TValue>> InvokeAsync(
        MessageContext<TKey, TValue> context,
        MessageHandler<TKey, TValue> next) {
        using var activity = ActivitySource.StartActivity("circuit_check", ActivityKind.Internal);

        //
        // SHORT-CIRCUIT if OPEN
        //
        if (_state == CircuitState.Open) {
            if (timeProvider.GetUtcNow() - _circuitOpenedTime > _openInterval) {
                _state = CircuitState.HalfOpen;
                activity?.AddEvent(new ActivityEvent("circuit_half_open"));
            } else {
                activity?.SetStatus(ActivityStatusCode.Error, "circuit_open");
                return context with { Failure = FailureType.DependencyDown };
            }
        }

        //
        // CALL NEXT MIDDLEWARE
        //
        var result = await next(context);

        //
        // SUCCESS PATH
        //
        if (result.Failure == FailureType.None) {
            if (_state == CircuitState.HalfOpen) {
                CloseCircuit(activity);
            }
            return result;
        }

        //
        // FAILURE CLASSIFICATION
        //
        var isDependencyFailure =
            result.Failure == FailureType.DependencyDown;

        //
        // HALF-OPEN probe failed → re-open immediately
        //
        if (_state == CircuitState.HalfOpen && isDependencyFailure) {
            OpenCircuit(activity);
            return result;
        }

        //
        // CLOSED but repeated dependency failures
        //
        if (isDependencyFailure) {
            RecordFailure(timeProvider.GetUtcNow());

            if (_recentFailures.Count >= _failureThreshold) {
                OpenCircuit(activity);
            }
        }

        return result;
    }

    //
    // FAILURE TRACKING
    //
    private void RecordFailure(DateTimeOffset now) {
        _recentFailures.Enqueue(now);

        while (_recentFailures.Count > 0 &&
               (now - _recentFailures.Peek()) > _rollingWindow) {
            _recentFailures.Dequeue();
        }
    }

    //
    // CIRCUIT TRANSITIONS
    //
    private void OpenCircuit(Activity? activity) {
        _state = CircuitState.Open;
        _circuitOpenedTime = timeProvider.GetUtcNow();
        activity?.AddEvent(new ActivityEvent("circuit_opened"));
        activity?.SetStatus(ActivityStatusCode.Error, "dependency_unavailable");
    }

    private void CloseCircuit(Activity? activity) {
        _state = CircuitState.Closed;
        _recentFailures.Clear();
        activity?.AddEvent(new ActivityEvent("circuit_closed"));
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private enum CircuitState {
        Closed,
        Open,
        HalfOpen
    }
}
