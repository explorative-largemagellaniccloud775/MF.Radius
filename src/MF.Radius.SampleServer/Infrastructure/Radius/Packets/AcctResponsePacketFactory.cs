using System.Buffers;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Models.Acct;
using MF.Radius.Core.Packets.Builders;
using MF.Radius.SampleServer.Application.Interfaces.Packets;

namespace MF.Radius.SampleServer.Infrastructure.Radius.Packets;

/// <summary>
/// Builds accounting response packets (Accounting-Response).
/// </summary>
public class AcctResponsePacketFactory
    : IAcctResponsePacketFactory
{
    
    public IMemoryOwner<byte> BuildAccountingResponse(RadiusAcctRequest request, string sharedSecret)
    {
        var responseDataOwner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);

        var builder = new RadiusPacketBuilder(
            responseDataOwner.Memory.Span,
            RadiusCode.AccountingResponse,
            request.RawPacket.Identifier,
            request.RawPacket.Authenticator.Span
        );
        builder.Complete(sharedSecret);
        
        return responseDataOwner;
    }
    
}
