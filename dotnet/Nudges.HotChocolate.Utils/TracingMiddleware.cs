using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using Nudges.Telemetry;

namespace Nudges.HotChocolate.Utils;

public class TracingMiddleware(FieldDelegate next) {
    private static readonly ActivitySource ActivitySource = new($"{typeof(TracingMiddleware).Namespace}.{nameof(TracingMiddleware)}");

    public async Task InvokeAsync(IMiddlewareContext context) {
        using var activity = ActivitySource.StartActivity(context.Selection.Field.Name, ActivityKind.Server,
            context.Service<IHttpContextAccessor>().HttpContext?.Request.GetActivityContext() ?? default);
        activity?.SetTag("graphql.operation", context.Operation.Name);
        activity?.SetTag("graphql.field", context.Selection.Field.Name);
        activity?.SetTag("graphql.path", context.Path.ToString());
        activity?.SetTag("graphql.selectionSet", context.Selection.SyntaxNode.ToString());

        try {
            await next(context);
            activity?.SetTag("graphql.status", "success");
        } catch (Exception ex) {
            activity?.SetTag("graphql.status", "error");
            activity?.SetTag("graphql.error", ex.Message);
            throw;
        }
    }
}
