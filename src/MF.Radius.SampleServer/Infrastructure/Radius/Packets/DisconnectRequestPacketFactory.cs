using System.Buffers;
using System.Security.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Packets.Builders;
using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;

namespace MF.Radius.SampleServer.Infrastructure.Radius.Packets;

public class DisconnectRequestPacketFactory
    : IDisconnectRequestPacketFactory
{
    
    public IMemoryOwner<byte> BuildDisconnectRequest(DisconnectSessionCommand command, string sharedSecret)
    {
        var owner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
        var id = (byte)RandomNumberGenerator.GetInt32(0, 256);

        var builder = new RadiusPacketBuilder(
            owner.Memory.Span,
            RadiusCode.DisconnectRequest,
            id
        );
        builder.ApplyDisconnect(command.SessionId, command.UserName);
        builder.Complete(sharedSecret);
        
        return owner;
    }
    
}
