using System.Buffers;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Auth;
using MF.Radius.SampleServer.Application.Options;
using MF.Radius.SampleServer.Domain.Entities;

namespace MF.Radius.SampleServer.Application.Interfaces.Packets;

/// <summary>
/// Defines methods for generating RADIUS authentication response packets,
/// specifically Access-Accept and Access-Reject responses.
/// </summary>
public interface IAuthResponsePacketFactory
{
    
    IMemoryOwner<byte> BuildAccessAccept(
        RadiusAuthRequestBase authRequest,
        string sharedSecret,
        Subscriber subscriber,
        RadiusIspOptions options
    );

    IMemoryOwner<byte> BuildAccessReject(
        RadiusPacket requestPacket,
        string sharedSecret
    );
    
}
