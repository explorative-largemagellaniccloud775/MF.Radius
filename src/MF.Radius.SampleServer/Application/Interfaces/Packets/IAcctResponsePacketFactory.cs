using System.Buffers;
using MF.Radius.Core.Models.Acct;

namespace MF.Radius.SampleServer.Application.Interfaces.Packets;

/// <summary>
/// Defines a factory for creating RADIUS accounting response packets.
/// </summary>
public interface IAcctResponsePacketFactory
{
    
    IMemoryOwner<byte> BuildAccountingResponse(
        RadiusAcctRequest request,
        string sharedSecret
    );
    
}