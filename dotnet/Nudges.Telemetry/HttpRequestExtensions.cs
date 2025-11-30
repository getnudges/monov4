using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Nudges.Telemetry;

public static class HttpRequestExtensions {
    public static ActivityContext GetActivityContext(this HttpRequest request) {
        if (request.Headers.TraceParent is StringValues parent && parent.FirstOrDefault() is string parentString) {
            if (request.Headers.TraceState is StringValues state && state.FirstOrDefault() is string stateString) {
                return ActivityContext.Parse(parentString, stateString);
            }
            return ActivityContext.Parse(parentString, default);
        }
        return Activity.Current?.Context ?? default;
    }
}
