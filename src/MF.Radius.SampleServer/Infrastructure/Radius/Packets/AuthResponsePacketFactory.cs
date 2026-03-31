using System.Buffers;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Auth;
using MF.Radius.Core.Packets.Builders;
using MF.Radius.SampleServer.Application.Interfaces.Packets;
using MF.Radius.SampleServer.Application.Options;
using MF.Radius.SampleServer.Domain.Entities;

namespace MF.Radius.SampleServer.Infrastructure.Radius.Packets;

/// <summary>
/// Builds authentication response packets for Access-Accept/Reject flows.
/// </summary>
public class AuthResponsePacketFactory
    : IAuthResponsePacketFactory
{
    
    public IMemoryOwner<byte> BuildAccessAccept(
        RadiusAuthRequestBase authRequest, 
        string sharedSecret, 
        Subscriber subscriber,
        RadiusIspOptions options
    )
    {
        var responseDataOwner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
        
        var builder = new RadiusPacketBuilder(
            responseDataOwner.Memory.Span,
            RadiusCode.AccessAccept,
            authRequest.RawPacket.Identifier,
            authRequest.RawPacket.Authenticator.Span
        );
        builder.ApplyStandardIspAttributes(options, subscriber.StaticIp);
        builder.ApplyCiscoRateLimit(subscriber.BaseRateLimit);
        builder.Complete(sharedSecret);
        
        return responseDataOwner;
    }

    public IMemoryOwner<byte> BuildAccessReject(
        RadiusPacket requestPacket, 
        string sharedSecret
    )
    {
        var responseDataOwner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);

        var builder = new RadiusPacketBuilder(
            responseDataOwner.Memory.Span,
            RadiusCode.AccessReject,
            requestPacket.Identifier,
            requestPacket.Authenticator.Span
        );
        builder.Complete(sharedSecret);
        
        return responseDataOwner;
    }
    
}