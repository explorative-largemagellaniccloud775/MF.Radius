using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Events;

namespace MF.Radius.SampleServer.Infrastructure.Events;

/// <summary>
/// Represents an event handler responsible for logging the completion of NAS commands.
/// </summary>
/// <remarks>
/// This handler listens for <see cref="NasCommandCompletedEvent"/> events and logs the outcome
/// including details such as command name, session ID, user name, status, and response code.
/// </remarks>
/// <example>
/// This handler is typically used in systems that process and monitor NAS commands,
/// providing detailed logs for auditing or debugging purposes.
/// </example>
/// <typeparam name="NasCommandCompletedEvent">
/// The type of application event this handler processes, in this case a completed NAS command event.
/// </typeparam>
public class NasCommandCompletedLoggingHandler(
    ILogger<NasCommandCompletedLoggingHandler> logger
)
    : IApplicationEventHandler<NasCommandCompletedEvent>
{
    
    public ValueTask HandleAsync(NasCommandCompletedEvent evt, CancellationToken ct)
    {
        logger.LogInformation(
            "NAS command completed: {CommandName}, SessionId={SessionId}, UserName={UserName}, Status={Status}, RadiusCode={RadiusCode}, Reason={Reason}",
            evt.CommandName,
            evt.SessionId,
            evt.UserName,
            evt.Result.Status,
            evt.Result.RadiusResponseCode,
            evt.Result.FailureReason
        );
        return ValueTask.CompletedTask;
    }
    
}
