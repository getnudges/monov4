using Polly;
using Polly.Wrap;

namespace GraphQLGateway;

public static class PollyPolicies {
    public static AsyncPolicyWrap<HttpResponseMessage> GetResiliencePolicy() {
        var circuitBreaker = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10));

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            );

        return Policy.WrapAsync(retryPolicy, circuitBreaker);
    }
}
