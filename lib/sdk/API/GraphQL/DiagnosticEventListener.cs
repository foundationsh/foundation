using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

namespace Foundation.Core.SDK.API.GraphQL;

public class DiagnosticEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<DiagnosticEventListener> Logger;

    public DiagnosticEventListener(ILogger<DiagnosticEventListener> logger)
        => Logger = logger;

    public override void RequestError(IRequestContext context,
        Exception exception)
    {
        context.Result = QueryResultBuilder.CreateError(
            ErrorBuilder.New()
                .SetMessage("[Foundation Diagnostics] Unexpected execution error.")
                .SetException(exception)
                .Build());

        Logger.LogError(exception, "[Diagnostic Event Listener] A request error occurred!");
    }
}
