using System.Net;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Extensions;
using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Models;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Infrastructure.Radius;

/// <summary>
/// A gateway for executing network access server (NAS) commands utilizing the RADIUS protocol.
/// This class provides functionality for disconnecting and restricting sessions through
/// interaction with a NAS device.
/// </summary>
/// <remarks>
/// The <c>RadiusNasCommandGateway</c> class is an implementation of the <see cref="INasCommandGateway"/>
/// interface. It relies on the following injected dependencies:
/// <list type="bullet">
/// <item><c>IRadiusSender</c>: Used to send RADIUS requests to the NAS.</item>
/// <item><c>IRadiusSharedSecretResolver</c>: Resolves shared secrets for authenticating requests to the NAS.</item>
/// <item><c>ICoaRequestPacketFactory</c>: Creates Change of Authorization (CoA) request packets.</item>
/// <item><c>IDisconnectRequestPacketFactory</c>: Constructs RADIUS request packets for session disconnection.</item>
/// </list>
/// </remarks>
public sealed class RadiusNasCommandGateway(
    IRadiusSender sender,
    IRadiusSharedSecretResolver secretResolver,
    ICoaRequestPacketFactory coaFactory,
    IDisconnectRequestPacketFactory disconnectFactory
)
    : INasCommandGateway
{
    
    public async ValueTask<NasCommandResult> DisconnectAsync(DisconnectSessionCommand command, CancellationToken ct)
    {
        var sharedSecret = await ResolveSecretAsync(command.NasEndPoint, ct);
        if (sharedSecret is null)
            return NasCommandResult.Failed(
                NasCommandFailureReason.SharedSecretNotFound, 
                "Shared secret was not resolved."
            );

        var requestDataOwner = disconnectFactory.BuildDisconnectRequest(command, sharedSecret);
        try
        {
            var responsePacket = await sender.SendAndReceiveAsync(requestDataOwner, command.NasEndPoint, sharedSecret, ct);
            return MapResponse(responsePacket);
            
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return NasCommandResult.Timeout();
        }
        catch (Exception ex)
        {
            return NasCommandResult.Failed(NasCommandFailureReason.TransportError, ex.Message);
        }
        
    }

    public async ValueTask<NasCommandResult> RestrictAsync(RestrictSessionCommand command, CancellationToken ct)
    {
        var sharedSecret = await ResolveSecretAsync(command.NasEndPoint, ct);
        if (sharedSecret is null)
            return NasCommandResult.Failed(
                NasCommandFailureReason.SharedSecretNotFound, 
                "Shared secret was not resolved."
            );

        var requestOwner = coaFactory.BuildAclRestrictionRequest(
            new ApplyAclRestrictionCommand
            {
                NasEndPoint = command.NasEndPoint,
                SessionId = command.SessionId,
                UserName = command.UserName,
                AclName = command.AclName
            },
            sharedSecret
        );

        try
        {
            var response = await sender.SendAndReceiveAsync(requestOwner, command.NasEndPoint, sharedSecret, ct);
            return MapResponse(response);
            
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return NasCommandResult.Timeout();
        }
        catch (Exception ex)
        {
            return NasCommandResult.Failed(NasCommandFailureReason.TransportError, ex.Message);
        }
        
    }

    private async ValueTask<string?> ResolveSecretAsync(EndPoint endPoint, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sharedSecret = await secretResolver.GetSharedSecretAsync(endPoint);
        return !string.IsNullOrWhiteSpace(sharedSecret)
            ? sharedSecret
            : null;
    }

    private static NasCommandResult MapResponse(RadiusPacket? responsePacket)
    {
        if (!responsePacket.HasValue)  return NasCommandResult.Timeout();
        var packet = responsePacket.Value;

        return packet.Code switch
        {
            RadiusCode.DisconnectAck or RadiusCode.CoAAck => NasCommandResult.Success(packet.Code, packet.Identifier),
            RadiusCode.DisconnectNak or RadiusCode.CoANak => NasCommandResult.Rejected(
                packet.Code,
                packet.Identifier,
                packet.GetNasErrorDescription(out _)
            ),
            _ => NasCommandResult.Failed(
                NasCommandFailureReason.UnexpectedResponseCode,
                "Unexpected RADIUS response code.", 
                packet.Code, 
                packet.Identifier
            )
        };
        
    }
    
}
