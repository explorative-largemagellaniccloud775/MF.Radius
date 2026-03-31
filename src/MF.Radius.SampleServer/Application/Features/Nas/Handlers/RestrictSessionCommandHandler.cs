using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Events;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Application.Features.Nas.Handlers;

/// <summary>
/// Handles the processing of the RestrictSessionCommand within the system.
/// </summary>
/// <remarks>
/// This handler is responsible for restricting a session based on the provided input parameters. It validates the
/// command's session ID and ACL name before proceeding to execute the restriction logic through the provided
/// INasCommandGateway. Upon completion, it publishes an event using IApplicationEventPublisher to notify other
/// components of the command's result.
/// </remarks>
/// <param name="gateway">
/// The gateway that provides the functionality to execute the restriction command.
/// </param>
/// <param name="eventPublisher">
/// The publisher responsible for broadcasting events related to the command handling process.
/// </param>
public sealed class RestrictSessionCommandHandler(
    INasCommandGateway gateway,
    IApplicationEventPublisher eventPublisher
)
    : ICommandHandler<RestrictSessionCommand, NasCommandResult>
{
    
    public async ValueTask<NasCommandResult> HandleAsync(RestrictSessionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.SessionId))
            return NasCommandResult.InvalidInput("SessionId is required.");
            
        if (string.IsNullOrWhiteSpace(command.AclName))
            return NasCommandResult.InvalidInput("AclName is required.");
        
        var result = await gateway.RestrictAsync(command, ct);
        await eventPublisher.PublishAsync(new NasCommandCompletedEvent
        {
            CommandName = nameof(RestrictSessionCommand),
            SessionId = command.SessionId,
            UserName = command.UserName,
            Result = result
        }, ct);
        
        return result;
    }
}