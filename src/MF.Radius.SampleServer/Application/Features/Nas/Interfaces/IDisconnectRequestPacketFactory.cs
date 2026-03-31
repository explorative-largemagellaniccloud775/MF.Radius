using System.Buffers;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;

namespace MF.Radius.SampleServer.Application.Features.Nas.Interfaces;

/// <summary>
/// Defines a factory for building disconnect request packets.
/// </summary>
public interface IDisconnectRequestPacketFactory
{
    
    IMemoryOwner<byte> BuildDisconnectRequest(
        DisconnectSessionCommand command,
        string sharedSecret
    );
    
}
