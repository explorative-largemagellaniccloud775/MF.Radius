using System.Net;
using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models.Auth;

/// <summary>
/// Base class representing a RADIUS authentication request. This class serves as the foundation
/// for various authentication protocols such as PAP, CHAP, and MS-CHAPv2.
/// </summary>
public abstract record RadiusAuthRequestBase
{
    /// <summary>The identity of the user (User-Name attribute).</summary>
    public required string UserName { get; init; }
    
    /// <summary>The original RADIUS packet for access to supplementary attributes.</summary>
    public required RadiusPacket RawPacket { get; init; }
    
    public required EndPoint RemoteEndPoint { get; init; }
    
    public abstract RadiusAuthProtocol AuthProtocol { get; }
    
}
