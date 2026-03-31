using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Events;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Application.Features.Nas.Handlers;

/// <summary>
/// Handles the processing of the <see cref="DisconnectSessionCommand"/> to disconnect a user's session
/// from the system via the Network Access Server (NAS).
/// </summary>
/// <remarks>
/// This command handler performs the following functions:
/// - Validates the <see cref="DisconnectSessionCommand"/>, ensuring that the required fields are present.
/// - Uses the <see cref="INasCommandGateway"/> to process the disconnect operation.
/// - Publishes an event via <see cref="IApplicationEventPublisher"/> after the command has been executed.
/// </remarks>
/// <param name="gateway">
/// The gateway responsible for communicating with the Network Access Server (NAS) to execute the disconnect action.
/// </param>
/// <param name="eventPublisher">
/// The event publisher used to publish events related to the command's completion.
/// </param>
public class DisconnectSessionCommandHandler(
    INasCommandGateway gateway,
    IApplicationEventPublisher eventPublisher
)
    : ICommandHandler<DisconnectSessionCommand, NasCommandResult>
{
    
    public async ValueTask<NasCommandResult> HandleAsync(DisconnectSessionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.SessionId))
            return NasCommandResult.InvalidInput("SessionId is required.");
        
        var result = await gateway.DisconnectAsync(command, ct);
        await eventPublisher.PublishAsync(new NasCommandCompletedEvent
        {
            CommandName = nameof(DisconnectSessionCommand),
            SessionId = command.SessionId,
            UserName = command.UserName,
            Result = result
        }, ct);

        return result;
    }
    
}
