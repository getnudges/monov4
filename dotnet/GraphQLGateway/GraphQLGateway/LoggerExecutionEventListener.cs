using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

namespace GraphQLGateway;

public class LoggerExecutionEventListener(ILogger<LoggerExecutionEventListener> logger) : ExecutionDiagnosticEventListener {
    public override void RequestError(IRequestContext context, Exception exception) {
        logger.LogRequestException(exception);
        base.RequestError(context, exception);
    }
}


