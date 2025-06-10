using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace ProductApi;
public class LoggerExecutionEventListener(ILogger<LoggerExecutionEventListener> logger) : ExecutionDiagnosticEventListener {

    public override void RequestError(IRequestContext context,
        Exception exception) => logger.LogGraphqlError(exception);
    public override IDisposable ExecuteRequest(IRequestContext context) {
        logger.LogDebug($"Executing request: {context.Exception?.Message}", context);
        return base.ExecuteRequest(context);
    }

    public override IDisposable ExecuteOperation(IRequestContext context) => base.ExecuteOperation(context);
    public override void ResolverError(IMiddlewareContext context, IError error) => base.ResolverError(context, error);
    public override void ResolverError(IRequestContext context, ISelection selection, IError error) => base.ResolverError(context, selection, error);
    public override void SyntaxError(IRequestContext context, IError error) => base.SyntaxError(context, error);
    public override void TaskError(IExecutionTask task, IError error) => base.TaskError(task, error);

    public override IDisposable ExecuteSubscription(ISubscription subscription) {
        logger.LogDebug($"Executing subscription: {subscription}", subscription);
        return base.ExecuteSubscription(subscription);
    }
}
