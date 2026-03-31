using System.Buffers;
using System.Security.Cryptography;
using MF.Radius.Core.Enums;
using MF.Radius.Core.Models;
using MF.Radius.Core.Packets.Builders;
using MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;
using MF.Radius.SampleServer.Application.Features.Nas.Interfaces;

namespace MF.Radius.SampleServer.Infrastructure.Radius.Packets;

public class CoaRequestCiscoPacketFactory
    : ICoaRequestPacketFactory
{
    
    public IMemoryOwner<byte> BuildAclRestrictionRequest(ApplyAclRestrictionCommand command, string sharedSecret)
    {
        var memoryOwner = CreateBaseCoa(command.SessionId, command.UserName, out var builder);
        builder.ApplyCiscoRestriction(command.SessionId, command.UserName, command.AclName);
        builder.Complete(sharedSecret);
        return memoryOwner;
    }

    public IMemoryOwner<byte> BuildRateLimitRequest(ApplyRateLimitCommand command, string sharedSecret)
    {
        var memoryOwner = CreateBaseCoa(command.SessionId, command.UserName, out var builder);
        builder.ApplyCiscoRateLimit(command.DownstreamKbps);
        builder.Complete(sharedSecret);
        return memoryOwner;
    }

    public IMemoryOwner<byte> BuildServiceProfileRequest(ApplyServiceProfileCommand command, string sharedSecret)
    {
        var memoryOwner = CreateBaseCoa(command.SessionId, command.UserName, out var builder);

        var vsa = builder.GetCiscoAttributeBuilder();
        vsa.AddAvPair("subscriber:command", "account-logon");
        builder.Apply(vsa);

        vsa = builder.GetCiscoAttributeBuilder();
        vsa.AddAvPair("subscriber:service-name", command.ProfileName);
        builder.Apply(vsa);

        builder.Complete(sharedSecret);
        return memoryOwner;
        
    }

    public IMemoryOwner<byte> BuildSessionPolicyRequest(ApplySessionPolicyCommand command, string sharedSecret)
    {
        var memoryOwner = CreateBaseCoa(command.SessionId, command.UserName, out var builder);

        var attrs = builder.GetAttributeBuilder();
        if (command.SessionTimeoutSeconds.HasValue)
            attrs.AddInt32(RadiusAttributeType.SessionTimeout, command.SessionTimeoutSeconds.Value);
        if (command.IdleTimeoutSeconds.HasValue)
            attrs.AddInt32(RadiusAttributeType.IdleTimeout, command.IdleTimeoutSeconds.Value);
        if (command.AcctInterimIntervalSeconds.HasValue)
            attrs.AddInt32(RadiusAttributeType.AcctInterimInterval, command.AcctInterimIntervalSeconds.Value);
        builder.Apply(attrs);

        builder.Complete(sharedSecret);
        return memoryOwner;
        
    }


    /// <summary>
    /// Creates a base CoA (Change of Authorization) request packet for RADIUS communication.
    /// </summary>
    /// <param name="sessionId">The unique session identifier used for the RADIUS operation.</param>
    /// <param name="userName">The username associated with the session. Can be null or empty if not applicable.</param>
    /// <param name="builder">An output parameter that provides the RadiusPacketBuilder instance for further customization of the request.</param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}"/> that holds the allocated memory for the packet.
    /// The caller is responsible for disposing of the memory when it is no longer needed.
    /// </returns>
    private static IMemoryOwner<byte> CreateBaseCoa(
        string sessionId,
        string? userName,
        out RadiusPacketBuilder builder
    )
    {
        var memoryOwner = MemoryPool<byte>.Shared.Rent(RadiusConstants.MaxPacketSize);
        var id = (byte)RandomNumberGenerator.GetInt32(0, 256);

        builder = new RadiusPacketBuilder(
            memoryOwner.Memory.Span,
            RadiusCode.CoARequest,
            id
        );

        // Session identification is mandatory for most NAS implementations.
        var attrs = builder.GetAttributeBuilder();
        attrs.AddText(RadiusAttributeType.AcctSessionId, sessionId);
        if (!string.IsNullOrWhiteSpace(userName))
            attrs.AddText(RadiusAttributeType.UserName, userName);
        builder.Apply(attrs);

        return memoryOwner;
    }
    
}
