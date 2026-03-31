using System.Buffers;
using MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;

namespace MF.Radius.SampleServer.Application.Features.Nas.Interfaces;

/// <summary>
/// Defines the factory interface for creating Change of Authorization (CoA) request packets
/// for various NAS (Network Access Server) operations. These operations include applying
/// ACL restrictions, rate limiting, service profiles, and session policies.
/// </summary>
public interface ICoaRequestPacketFactory
{
    // Example: filter-spec / ACL
    IMemoryOwner<byte> BuildAclRestrictionRequest(ApplyAclRestrictionCommand command, string sharedSecret);
    
    // Example: rate shaping
    IMemoryOwner<byte> BuildRateLimitRequest(ApplyRateLimitCommand command, string sharedSecret);
    
    // Example: service/profile switch
    IMemoryOwner<byte> BuildServiceProfileRequest(ApplyServiceProfileCommand command, string sharedSecret);
    
    // Example: SessionTimeout, IdleTimeout, InterimInterval
    IMemoryOwner<byte> BuildSessionPolicyRequest(ApplySessionPolicyCommand command, string sharedSecret);
    
}
