using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution;

namespace UserApi;
public class LoggerExecutionEventListener(ILogger<LoggerExecutionEventListener> logger) : ExecutionDiagnosticEventListener {
    public override void RequestError(IRequestContext context,
        Exception exception) => logger.LogGraphqlError(exception);
}
